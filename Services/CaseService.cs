using AILegalAsst.Models;
using System.Text.Json;

namespace AILegalAsst.Services;

/// <summary>
/// Service for managing cases with JSON file persistence
/// </summary>
public class CaseService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CaseService> _logger;
    private readonly string _casesFilePath;
    private readonly object _fileLock = new();
    private CasesJsonRoot _casesData = new();

    public CaseService(IWebHostEnvironment environment, ILogger<CaseService> logger)
    {
        _environment = environment;
        _logger = logger;
        _casesFilePath = Path.Combine(_environment.WebRootPath, "data", "cases.json");
        
        // Ensure data directory exists
        var dataDir = Path.GetDirectoryName(_casesFilePath);
        if (!string.IsNullOrEmpty(dataDir) && !Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }

        LoadCasesFromFile();
    }

    private void LoadCasesFromFile()
    {
        lock (_fileLock)
        {
            try
            {
                if (File.Exists(_casesFilePath))
                {
                    var json = File.ReadAllText(_casesFilePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    _casesData = JsonSerializer.Deserialize<CasesJsonRoot>(json, options) ?? new CasesJsonRoot();
                    _logger.LogInformation("Loaded {Count} cases from JSON file", _casesData.Cases.Count);
                }
                else
                {
                    _casesData = new CasesJsonRoot
                    {
                        Cases = new List<CaseDto>(),
                        LastUpdated = DateTime.UtcNow,
                        NextId = 1
                    };
                    SaveCasesToFile();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cases from file");
                _casesData = new CasesJsonRoot { Cases = new(), NextId = 1 };
            }
        }
    }

    private void SaveCasesToFile()
    {
        lock (_fileLock)
        {
            try
            {
                _casesData.LastUpdated = DateTime.UtcNow;
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var json = JsonSerializer.Serialize(_casesData, options);
                File.WriteAllText(_casesFilePath, json);
                _logger.LogInformation("Saved {Count} cases to JSON file", _casesData.Cases.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving cases to file");
            }
        }
    }

    public Task<List<Case>> GetAllCasesAsync()
    {
        var cases = _casesData.Cases.Select(MapToCase).ToList();
        return Task.FromResult(cases);
    }

    public Task<List<Case>> GetCasesByRoleAsync(UserRole role, string userEmail)
    {
        List<Case> cases;
        
        // Filter based on role
        if (role == UserRole.Citizen)
        {
            // Citizens see only their own cases
            cases = _casesData.Cases
                .Where(c => c.ComplainantEmail.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
                .Select(MapToCase)
                .ToList();
        }
        else if (role == UserRole.Admin)
        {
            // Admin sees all cases
            cases = _casesData.Cases.Select(MapToCase).ToList();
        }
        else
        {
            // Police and Lawyers see all cases (can be customized later for assignments)
            cases = _casesData.Cases.Select(MapToCase).ToList();
        }
        
        return Task.FromResult(cases);
    }

    public Task<List<Case>> GetCasesByUserAsync(string userEmail)
    {
        var cases = _casesData.Cases
            .Where(c => c.ComplainantEmail.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
            .Select(MapToCase)
            .ToList();
        return Task.FromResult(cases);
    }

    public Task<Case?> GetCaseByIdAsync(int id)
    {
        var caseDto = _casesData.Cases.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(caseDto != null ? MapToCase(caseDto) : null);
    }

    public Task<CaseDto?> GetCaseDtoByIdAsync(int id)
    {
        var caseDto = _casesData.Cases.FirstOrDefault(c => c.Id == id);
        return Task.FromResult(caseDto);
    }

    public Task<List<Case>> GetCybercrimeeCasesAsync()
    {
        var cases = _casesData.Cases
            .Where(c => c.IsCybercrime)
            .Select(MapToCase)
            .ToList();
        return Task.FromResult(cases);
    }

    public Task<Case> CreateCaseAsync(Case newCase)
    {
        var caseDto = MapToCaseDto(newCase);
        caseDto.Id = _casesData.NextId++;
        caseDto.CaseNumber = GenerateCaseNumber(newCase.Type, newCase.IsCybercrime);
        caseDto.FiledDate = DateTime.UtcNow;

        // Initialize workflow with Filed step
        caseDto.Workflow = new List<CaseWorkflowStep>
        {
            new CaseWorkflowStep
            {
                Stage = WorkflowStages.Filed,
                Status = "Completed",
                Date = DateTime.UtcNow,
                Actor = newCase.Complainant,
                ActorRole = "Citizen",
                Notes = "Complaint filed via AI Legal Assistant"
            }
        };

        _casesData.Cases.Add(caseDto);
        SaveCasesToFile();

        return Task.FromResult(MapToCase(caseDto));
    }

    public Task<Case> CreateCaseFromAgentAsync(CaseDto caseDto, string complainantName, string complainantEmail)
    {
        caseDto.Id = _casesData.NextId++;
        caseDto.CaseNumber = GenerateCaseNumber(
            Enum.TryParse<CaseType>(caseDto.Type, out var type) ? type : CaseType.Criminal,
            caseDto.IsCybercrime);
        caseDto.FiledDate = DateTime.UtcNow;
        caseDto.Complainant = complainantName;
        caseDto.ComplainantEmail = complainantEmail;
        caseDto.Status = "Filed";
        caseDto.FiledViaAI = true;

        // Initialize workflow with AI attribution
        caseDto.Workflow = new List<CaseWorkflowStep>
        {
            new CaseWorkflowStep
            {
                Stage = WorkflowStages.Filed,
                Status = "Completed",
                Date = DateTime.UtcNow,
                Actor = complainantName,
                ActorRole = "Citizen",
                Notes = "Complaint filed via AI Agent Assistant (Identity Verified)",
                IsAIInitiated = true,
                VerifiedByUserId = caseDto.FiledByUserId?.ToString()
            }
        };

        _casesData.Cases.Add(caseDto);
        SaveCasesToFile();

        return Task.FromResult(MapToCase(caseDto));
    }

    public Task<bool> UpdateCaseAsync(Case updatedCase)
    {
        var existingCase = _casesData.Cases.FirstOrDefault(c => c.Id == updatedCase.Id);
        if (existingCase != null)
        {
            var index = _casesData.Cases.IndexOf(existingCase);
            var updatedDto = MapToCaseDto(updatedCase);
            updatedDto.Workflow = existingCase.Workflow; // Preserve workflow
            updatedDto.LastUpdated = DateTime.UtcNow;
            _casesData.Cases[index] = updatedDto;
            SaveCasesToFile();
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> UpdateCaseWorkflowAsync(int caseId, CaseWorkflowStep newStep)
    {
        var existingCase = _casesData.Cases.FirstOrDefault(c => c.Id == caseId);
        if (existingCase != null)
        {
            existingCase.Workflow.Add(newStep);
            existingCase.LastUpdated = DateTime.UtcNow;
            
            // Update status based on workflow stage
            existingCase.Status = GetStatusFromWorkflow(newStep.Stage);
            
            SaveCasesToFile();
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<List<CaseWorkflowStep>> GetCaseWorkflowAsync(int caseId)
    {
        var caseDto = _casesData.Cases.FirstOrDefault(c => c.Id == caseId);
        return Task.FromResult(caseDto?.Workflow ?? new List<CaseWorkflowStep>());
    }

    public Task<List<Case>> SearchCasesAsync(string query)
    {
        var results = _casesData.Cases.Where(c =>
            c.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            c.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            c.CaseNumber.Contains(query, StringComparison.OrdinalIgnoreCase)
        ).Select(MapToCase).ToList();

        return Task.FromResult(results);
    }

    private string GenerateCaseNumber(CaseType type, bool isCybercrime)
    {
        var prefix = isCybercrime ? "CYB" : type switch
        {
            CaseType.Criminal => "CRM",
            CaseType.Civil => "CVL",
            CaseType.Constitutional => "CON",
            _ => "GEN"
        };
        return $"{prefix}/{DateTime.Now.Year}/{_casesData.NextId:D3}";
    }

    private string GetStatusFromWorkflow(string stage)
    {
        return stage switch
        {
            WorkflowStages.Filed => "Filed",
            WorkflowStages.FIRRegistered => "Filed",
            WorkflowStages.Investigation => "UnderInvestigation",
            WorkflowStages.EvidenceCollection => "UnderInvestigation",
            WorkflowStages.ChargesheetFiled => "ChargesheetFiled",
            WorkflowStages.LawyerAssigned => "InProgress",
            WorkflowStages.CourtHearing => "TrialInProgress",
            WorkflowStages.Judgement => "Judgement",
            WorkflowStages.CaseClosed => "Closed",
            _ => "Filed"
        };
    }

    private Case MapToCase(CaseDto dto)
    {
        return new Case
        {
            Id = dto.Id,
            CaseNumber = dto.CaseNumber,
            Title = dto.Title,
            Description = dto.Description,
            Type = Enum.TryParse<CaseType>(dto.Type, out var type) ? type : CaseType.Other,
            Status = Enum.TryParse<CaseStatus>(dto.Status, out var status) ? status : CaseStatus.Filed,
            FiledDate = dto.FiledDate,
            LastUpdated = dto.LastUpdated,
            Complainant = dto.Complainant,
            Accused = dto.Accused,
            Plaintiff = dto.Plaintiff,
            Defendant = dto.Defendant,
            LawyerName = dto.LawyerName,
            Court = dto.Court,
            NextHearingDate = dto.NextHearingDate,
            ApplicableLaws = dto.ApplicableLaws,
            Sections = dto.Sections,
            ApplicableSections = dto.Sections,
            FIRNumber = dto.FirNumber,
            PoliceStation = dto.PoliceStation,
            AssignedLawyer = dto.AssignedLawyer,
            InvestigatingOfficer = dto.InvestigatingOfficer,
            IsCybercrime = dto.IsCybercrime,
            CybercrimeCategory = dto.CybercrimeCategory,
            DigitalEvidence = dto.DigitalEvidence,
            DigitalEvidenceCollected = dto.DigitalEvidenceCollected,
            AIAnalysis = dto.AIAnalysis,
            SuccessProbability = dto.SuccessProbability,
            // AI Action Attribution
            FiledViaAI = dto.FiledViaAI,
            AIActionHash = dto.AIActionHash,
            FiledByUserId = dto.FiledByUserId,
            FiledByUserEmail = dto.FiledByUserEmail,
            IdentityVerificationMethod = dto.IdentityVerificationMethod,
            IdentityVerifiedAt = dto.IdentityVerifiedAt,
            DeviceFingerprint = dto.DeviceFingerprint,
            FilingSessionId = dto.FilingSessionId
        };
    }

    private CaseDto MapToCaseDto(Case case_)
    {
        return new CaseDto
        {
            Id = case_.Id,
            CaseNumber = case_.CaseNumber,
            Title = case_.Title,
            Description = case_.Description,
            Type = case_.Type.ToString(),
            Status = case_.Status.ToString(),
            FiledDate = case_.FiledDate,
            LastUpdated = case_.LastUpdated,
            Complainant = case_.Complainant,
            Accused = case_.Accused,
            Plaintiff = case_.Plaintiff,
            Defendant = case_.Defendant,
            LawyerName = case_.LawyerName,
            Court = case_.Court,
            NextHearingDate = case_.NextHearingDate,
            ApplicableLaws = case_.ApplicableLaws,
            Sections = case_.Sections ?? case_.ApplicableSections,
            FirNumber = case_.FIRNumber,
            PoliceStation = case_.PoliceStation,
            AssignedLawyer = case_.AssignedLawyer,
            InvestigatingOfficer = case_.InvestigatingOfficer,
            IsCybercrime = case_.IsCybercrime,
            CybercrimeCategory = case_.CybercrimeCategory,
            DigitalEvidence = case_.DigitalEvidence,
            DigitalEvidenceCollected = case_.DigitalEvidenceCollected,
            AIAnalysis = case_.AIAnalysis,
            SuccessProbability = case_.SuccessProbability,
            // AI Action Attribution
            FiledViaAI = case_.FiledViaAI,
            AIActionHash = case_.AIActionHash,
            FiledByUserId = case_.FiledByUserId,
            FiledByUserEmail = case_.FiledByUserEmail,
            IdentityVerificationMethod = case_.IdentityVerificationMethod,
            IdentityVerifiedAt = case_.IdentityVerifiedAt,
            DeviceFingerprint = case_.DeviceFingerprint,
            FilingSessionId = case_.FilingSessionId
        };
    }
}
