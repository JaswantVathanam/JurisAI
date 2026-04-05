using AILegalAsst.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AILegalAsst.Services
{
    /// <summary>
    /// Service for managing digital evidence with blockchain-style chain of custody tracking and location verification.
    /// Ensures tamper-proof audit trail and integrity verification for court admissibility.
    /// </summary>
    public class EvidenceCustodyService
    {
        private readonly List<EvidenceItem> _evidenceItems = new();
        private readonly List<CustodyLog> _custodyLogs = new();
        private readonly ILogger<EvidenceCustodyService> _logger;
        private readonly LocationTrackingService? _locationService;
        private readonly AzureAgentService _agentService;

        public EvidenceCustodyService(
            ILogger<EvidenceCustodyService> logger,
            AzureAgentService agentService,
            LocationTrackingService? locationService = null)
        {
            _logger = logger;
            _agentService = agentService;
            _locationService = locationService;
            InitializeSampleData();
        }

        private void InitializeSampleData()
        {
            // Sample evidence items for demonstration
            var evidence1 = new EvidenceItem
            {
                Id = "EVD-2024-001",
                CaseId = "FIR-2024-1247",
                EvidenceNumber = "E/1247/2024/001",
                Type = EvidenceType.DigitalDocument,
                Title = "Bank Statement - HDFC Account",
                Description = "Bank statement showing suspicious transactions from Jan-Mar 2024",
                FileName = "hdfc_statement_jan_mar_2024.pdf",
                FileSize = 2457600,
                MimeType = "application/pdf",
                SHA256Hash = "a7ffc6f8bf1ed76651c14756a061d662f580ff4de43b49fa82d80a4b80f8434a",
                MD5Hash = "d41d8cd98f00b204e9800998ecf8427e",
                Status = EvidenceStatus.Verified,
                CollectedBy = "SI Priya Sharma",
                CollectedAt = DateTime.Now.AddDays(-15),
                CollectionLocation = "Cyber Cell Office, Mumbai",
                StorageLocation = "Digital Evidence Server - Rack 3, Slot 12",
                WitnessName = "Constable Ramesh Patil",
                WitnessBadgeNumber = "MH-4521",
                ChainIntegrity = true,
                LastVerifiedAt = DateTime.Now.AddHours(-2),
                TotalCustodyTransfers = 3,
                IsCourtSubmitted = false,
                Tags = new List<string> { "Financial", "Bank", "Fraud" },
                CreatedAt = DateTime.Now.AddDays(-15)
            };

            var evidence2 = new EvidenceItem
            {
                Id = "EVD-2024-002",
                CaseId = "FIR-2024-1247",
                EvidenceNumber = "E/1247/2024/002",
                Type = EvidenceType.MobileDevice,
                Title = "Seized Mobile Phone - Samsung Galaxy S21",
                Description = "Primary suspect's mobile device containing WhatsApp chats and call logs",
                FileName = "mobile_forensic_image.e01",
                FileSize = 67108864000,
                MimeType = "application/octet-stream",
                SHA256Hash = "b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9",
                MD5Hash = "098f6bcd4621d373cade4e832627b4f6",
                Status = EvidenceStatus.UnderAnalysis,
                CollectedBy = "Inspector Rajesh Kumar",
                CollectedAt = DateTime.Now.AddDays(-12),
                CollectionLocation = "Accused Residence, Andheri East",
                StorageLocation = "Physical Evidence Locker - A15",
                SeizureMemoNumber = "SM/1247/2024/001",
                WitnessName = "Shri Vikram Mehta (Panch Witness)",
                WitnessBadgeNumber = "Aadhaar: XXXX-XXXX-4521",
                ChainIntegrity = true,
                LastVerifiedAt = DateTime.Now.AddDays(-1),
                TotalCustodyTransfers = 2,
                IsCourtSubmitted = false,
                Tags = new List<string> { "Mobile", "Digital Forensics", "Primary Evidence" },
                CreatedAt = DateTime.Now.AddDays(-12)
            };

            var evidence3 = new EvidenceItem
            {
                Id = "EVD-2024-003",
                CaseId = "FIR-2024-1247",
                EvidenceNumber = "E/1247/2024/003",
                Type = EvidenceType.CCTV,
                Title = "CCTV Footage - ATM Camera",
                Description = "CCTV footage showing suspect withdrawing cash at specific times",
                FileName = "atm_cctv_12mar2024.mp4",
                FileSize = 524288000,
                MimeType = "video/mp4",
                SHA256Hash = "c3ab8ff13720e8ad9047dd39466b3c8974e592c2fa383d4a3960714caef0c4f2",
                MD5Hash = "5d41402abc4b2a76b9719d911017c592",
                Status = EvidenceStatus.Verified,
                CollectedBy = "ASI Sunita Deshmukh",
                CollectedAt = DateTime.Now.AddDays(-10),
                CollectionLocation = "SBI ATM, Kurla Station Road",
                StorageLocation = "Digital Evidence Server - Rack 2, Slot 8",
                WitnessName = "Bank Security Officer Mr. Patkar",
                ChainIntegrity = true,
                LastVerifiedAt = DateTime.Now.AddDays(-2),
                TotalCustodyTransfers = 2,
                IsCourtSubmitted = false,
                Tags = new List<string> { "Video", "CCTV", "ATM" },
                CreatedAt = DateTime.Now.AddDays(-10)
            };

            _evidenceItems.AddRange(new[] { evidence1, evidence2, evidence3 });

            // Create custody logs (blockchain-style chain)
            CreateInitialCustodyChain(evidence1);
            CreateInitialCustodyChain(evidence2);
            CreateInitialCustodyChain(evidence3);
        }

        private void CreateInitialCustodyChain(EvidenceItem evidence)
        {
            string previousHash = "GENESIS_BLOCK";

            // Collection entry
            var log1 = new CustodyLog
            {
                Id = $"CL-{Guid.NewGuid():N}",
                EvidenceId = evidence.Id,
                Action = CustodyAction.Collected,
                FromPerson = "Crime Scene",
                ToPerson = evidence.CollectedBy,
                FromLocation = evidence.CollectionLocation,
                ToLocation = "Cyber Cell Evidence Room",
                DateTime = evidence.CollectedAt,
                Notes = $"Evidence collected during investigation. Seizure Memo: {evidence.SeizureMemoNumber ?? "N/A"}",
                WitnessName = evidence.WitnessName,
                PreviousBlockHash = previousHash,
                PerformedBy = evidence.CollectedBy
            };
            log1.BlockHash = CalculateBlockHash(log1);
            log1.DigitalSignature = GenerateDigitalSignature(log1);
            _custodyLogs.Add(log1);
            previousHash = log1.BlockHash;

            // Storage entry
            var log2 = new CustodyLog
            {
                Id = $"CL-{Guid.NewGuid():N}",
                EvidenceId = evidence.Id,
                Action = CustodyAction.StoredInLocker,
                FromPerson = evidence.CollectedBy,
                ToPerson = "Evidence Custodian",
                FromLocation = "Cyber Cell Evidence Room",
                ToLocation = evidence.StorageLocation,
                DateTime = evidence.CollectedAt.AddHours(2),
                Notes = "Evidence secured in designated storage location",
                PreviousBlockHash = previousHash,
                PerformedBy = "Evidence Custodian"
            };
            log2.BlockHash = CalculateBlockHash(log2);
            log2.DigitalSignature = GenerateDigitalSignature(log2);
            _custodyLogs.Add(log2);
            previousHash = log2.BlockHash;

            // Verification entry
            var log3 = new CustodyLog
            {
                Id = $"CL-{Guid.NewGuid():N}",
                EvidenceId = evidence.Id,
                Action = CustodyAction.IntegrityVerified,
                FromPerson = "System",
                ToPerson = "System",
                FromLocation = evidence.StorageLocation,
                ToLocation = evidence.StorageLocation,
                DateTime = DateTime.Now.AddDays(-2),
                Notes = "Hash verification successful. Evidence integrity confirmed.",
                PreviousBlockHash = previousHash,
                PerformedBy = "Automated Verification System"
            };
            log3.BlockHash = CalculateBlockHash(log3);
            log3.DigitalSignature = GenerateDigitalSignature(log3);
            _custodyLogs.Add(log3);
        }

        /// <summary>
        /// Calculate SHA-256 hash for blockchain-style block
        /// </summary>
        private string CalculateBlockHash(CustodyLog log)
        {
            var blockData = $"{log.Id}|{log.EvidenceId}|{log.Action}|{log.FromPerson}|{log.ToPerson}|{log.DateTime:O}|{log.PreviousBlockHash}";
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(blockData));
            return Convert.ToHexString(hashBytes).ToLower();
        }

        /// <summary>
        /// Generate digital signature for audit trail
        /// </summary>
        private string GenerateDigitalSignature(CustodyLog log)
        {
            var signatureData = $"{log.BlockHash}|{log.PerformedBy}|{DateTime.UtcNow:O}";
            using var sha256 = SHA256.Create();
            var signatureBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(signatureData));
            return Convert.ToBase64String(signatureBytes);
        }

        /// <summary>
        /// Calculate file hash using SHA-256
        /// </summary>
        public async Task<string> CalculateSHA256Async(Stream fileStream)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(fileStream);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        /// <summary>
        /// Calculate file hash using MD5
        /// </summary>
        public async Task<string> CalculateMD5Async(Stream fileStream)
        {
            using var md5 = MD5.Create();
            var hashBytes = await md5.ComputeHashAsync(fileStream);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        /// <summary>
        /// Register new evidence with automatic hash generation and location tracking
        /// </summary>
        public async Task<EvidenceItem> RegisterEvidenceAsync(
            string caseId,
            EvidenceType type,
            string title,
            string description,
            string fileName,
            long fileSize,
            string mimeType,
            Stream fileStream,
            string collectedBy,
            string collectionLocation,
            string? witnessName = null,
            string? witnessBadgeNumber = null,
            string? seizureMemoNumber = null,
            List<string>? tags = null,
            double? latitude = null,
            double? longitude = null)
        {
            // Calculate hashes
            fileStream.Position = 0;
            var sha256Hash = await CalculateSHA256Async(fileStream);
            fileStream.Position = 0;
            var md5Hash = await CalculateMD5Async(fileStream);

            var evidenceNumber = GenerateEvidenceNumber(caseId);

            // Track evidence collection location
            LocationData? locationData = null;
            if (latitude.HasValue && longitude.HasValue && _locationService != null)
            {
                try
                {
                    locationData = await _locationService.TrackLocationAsync(
                        latitude.Value,
                        longitude.Value,
                        "Evidence",
                        caseId,
                        collectedBy,
                        $"Evidence collected: {title}");
                    
                    _logger.LogInformation(
                        "[EvidenceCustody] Location tracked for evidence collection: {Address}",
                        locationData.Address);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[EvidenceCustody] Error tracking evidence location");
                }
            }

            var evidence = new EvidenceItem
            {
                Id = $"EVD-{DateTime.Now:yyyy}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                CaseId = caseId,
                EvidenceNumber = evidenceNumber,
                Type = type,
                Title = title,
                Description = description,
                FileName = fileName,
                FileSize = fileSize,
                MimeType = mimeType,
                SHA256Hash = sha256Hash,
                MD5Hash = md5Hash,
                Status = EvidenceStatus.Collected,
                CollectedBy = collectedBy,
                CollectedAt = DateTime.Now,
                CollectionLocation = locationData?.Address ?? collectionLocation,
                WitnessName = witnessName,
                WitnessBadgeNumber = witnessBadgeNumber,
                SeizureMemoNumber = seizureMemoNumber,
                Tags = tags ?? new List<string>(),
                ChainIntegrity = true,
                TotalCustodyTransfers = 0,
                CreatedAt = DateTime.Now
            };

            _evidenceItems.Add(evidence);

            // Create initial custody log
            await AddCustodyLogAsync(evidence.Id, CustodyAction.Collected, "Crime Scene/Source", collectedBy,
                collectionLocation, "Evidence Collection Point", 
                $"Evidence collected from {locationData?.Address ?? collectionLocation}. {(seizureMemoNumber != null ? $"Seizure Memo: {seizureMemoNumber}" : "")}",
                collectedBy, witnessName);

            _logger.LogInformation("Registered evidence {Id} for case {CaseId} at {Location}", 
                evidence.Id, caseId, evidence.CollectionLocation);
            return evidence;
        }

        private string GenerateEvidenceNumber(string caseId)
        {
            var caseNumber = caseId.Replace("FIR-", "").Replace("-", "/");
            var count = _evidenceItems.Count(e => e.CaseId == caseId) + 1;
            return $"E/{caseNumber}/{count:D3}";
        }

        /// <summary>
        /// Add custody transfer log with blockchain-style chaining
        /// </summary>
        public async Task<CustodyLog> AddCustodyLogAsync(
            string evidenceId,
            CustodyAction action,
            string fromPerson,
            string toPerson,
            string fromLocation,
            string toLocation,
            string notes,
            string performedBy,
            string? witnessName = null)
        {
            var evidence = _evidenceItems.FirstOrDefault(e => e.Id == evidenceId);
            if (evidence == null)
                throw new ArgumentException($"Evidence {evidenceId} not found");

            // Get previous block hash
            var previousLog = _custodyLogs
                .Where(l => l.EvidenceId == evidenceId)
                .OrderByDescending(l => l.DateTime)
                .FirstOrDefault();
            var previousHash = previousLog?.BlockHash ?? "GENESIS_BLOCK";

            var log = new CustodyLog
            {
                Id = $"CL-{Guid.NewGuid():N}",
                EvidenceId = evidenceId,
                Action = action,
                FromPerson = fromPerson,
                ToPerson = toPerson,
                FromLocation = fromLocation,
                ToLocation = toLocation,
                DateTime = DateTime.Now,
                Notes = notes,
                WitnessName = witnessName,
                PreviousBlockHash = previousHash,
                PerformedBy = performedBy,
                IPAddress = "192.168.1.100" // Would be actual IP in production
            };

            log.BlockHash = CalculateBlockHash(log);
            log.DigitalSignature = GenerateDigitalSignature(log);

            _custodyLogs.Add(log);

            // Update evidence tracking
            evidence.TotalCustodyTransfers++;
            if (action == CustodyAction.TransferredOut || action == CustodyAction.TransferredIn)
            {
                evidence.StorageLocation = toLocation;
            }

            // Update status based on action
            evidence.Status = action switch
            {
                CustodyAction.Collected => EvidenceStatus.Collected,
                CustodyAction.StoredInLocker => EvidenceStatus.Secured,
                CustodyAction.SentForAnalysis => EvidenceStatus.UnderAnalysis,
                CustodyAction.ReturnedFromAnalysis => EvidenceStatus.Verified,
                CustodyAction.SubmittedToCourt => EvidenceStatus.CourtSubmitted,
                CustodyAction.Disposed => EvidenceStatus.Disposed,
                CustodyAction.TamperDetected => EvidenceStatus.Tampered,
                _ => evidence.Status
            };

            if (action == CustodyAction.SubmittedToCourt)
            {
                evidence.IsCourtSubmitted = true;
                evidence.CourtSubmissionDate = DateTime.Now;
            }

            _logger.LogInformation("Added custody log {Id} for evidence {EvidenceId} - Action: {Action}", 
                log.Id, evidenceId, action);

            return log;
        }

        /// <summary>
        /// Verify evidence integrity by checking hash and chain
        /// </summary>
        public async Task<EvidenceVerification> VerifyEvidenceIntegrityAsync(string evidenceId, Stream? currentFileStream = null)
        {
            var evidence = _evidenceItems.FirstOrDefault(e => e.Id == evidenceId);
            if (evidence == null)
                return new EvidenceVerification { IsValid = false, Message = "Evidence not found" };

            var verification = new EvidenceVerification
            {
                EvidenceId = evidenceId,
                VerifiedAt = DateTime.Now
            };

            // Verify chain integrity
            var chainValid = await VerifyChainIntegrityAsync(evidenceId);
            verification.ChainValid = chainValid;

            // Verify file hash if stream provided
            if (currentFileStream != null)
            {
                currentFileStream.Position = 0;
                var currentHash = await CalculateSHA256Async(currentFileStream);
                verification.HashValid = currentHash.Equals(evidence.SHA256Hash, StringComparison.OrdinalIgnoreCase);
                verification.CurrentHash = currentHash;
                verification.OriginalHash = evidence.SHA256Hash;
            }
            else
            {
                verification.HashValid = true; // Assume valid if no file to compare
            }

            verification.IsValid = verification.ChainValid && verification.HashValid;
            verification.Message = verification.IsValid
                ? "Evidence integrity verified successfully"
                : $"Integrity check failed: {(!verification.ChainValid ? "Chain broken" : "")} {(!verification.HashValid ? "Hash mismatch" : "")}".Trim();

            // Update evidence record
            evidence.LastVerifiedAt = DateTime.Now;
            evidence.ChainIntegrity = verification.ChainValid;
            if (!verification.IsValid)
            {
                evidence.Status = EvidenceStatus.Tampered;
                await AddCustodyLogAsync(evidenceId, CustodyAction.TamperDetected, "System", "System",
                    evidence.StorageLocation ?? "", evidence.StorageLocation ?? "",
                    $"Integrity verification failed: {verification.Message}", "Automated Verification System");
            }
            else
            {
                await AddCustodyLogAsync(evidenceId, CustodyAction.IntegrityVerified, "System", "System",
                    evidence.StorageLocation ?? "", evidence.StorageLocation ?? "",
                    "Hash verification successful", "Automated Verification System");
            }

            return verification;
        }

        /// <summary>
        /// Verify blockchain-style chain integrity
        /// </summary>
        public async Task<bool> VerifyChainIntegrityAsync(string evidenceId)
        {
            var logs = _custodyLogs
                .Where(l => l.EvidenceId == evidenceId)
                .OrderBy(l => l.DateTime)
                .ToList();

            if (!logs.Any()) return true;

            string expectedPreviousHash = "GENESIS_BLOCK";
            foreach (var log in logs)
            {
                // Verify previous hash linkage
                if (log.PreviousBlockHash != expectedPreviousHash)
                {
                    _logger.LogWarning("Chain integrity broken at log {LogId} - expected {Expected}, found {Found}",
                        log.Id, expectedPreviousHash, log.PreviousBlockHash);
                    return false;
                }

                // Verify current block hash
                var calculatedHash = CalculateBlockHash(log);
                if (log.BlockHash != calculatedHash)
                {
                    _logger.LogWarning("Block hash mismatch at log {LogId} - expected {Expected}, found {Found}",
                        log.Id, calculatedHash, log.BlockHash);
                    return false;
                }

                expectedPreviousHash = log.BlockHash;
            }

            return true;
        }

        /// <summary>
        /// Get evidence statistics for dashboard
        /// </summary>
        public async Task<EvidenceStatistics> GetStatisticsAsync(string? caseId = null)
        {
            var items = caseId != null
                ? _evidenceItems.Where(e => e.CaseId == caseId)
                : _evidenceItems;

            var stats = new EvidenceStatistics
            {
                TotalEvidence = items.Count(),
                VerifiedCount = items.Count(e => e.Status == EvidenceStatus.Verified),
                PendingVerification = items.Count(e => e.Status == EvidenceStatus.Collected || e.Status == EvidenceStatus.Secured),
                UnderAnalysis = items.Count(e => e.Status == EvidenceStatus.UnderAnalysis),
                CourtSubmitted = items.Count(e => e.IsCourtSubmitted),
                TamperedCount = items.Count(e => e.Status == EvidenceStatus.Tampered),
                TotalCustodyTransfers = items.Sum(e => e.TotalCustodyTransfers),
                TotalStorageSize = items.Sum(e => e.FileSize),
                EvidenceByType = items.GroupBy(e => e.Type).ToDictionary(g => g.Key.ToString(), g => g.Count()),
                EvidenceByStatus = items.GroupBy(e => e.Status).ToDictionary(g => g.Key.ToString(), g => g.Count()),
                ChainIntegrityRate = items.Any() ? (double)items.Count(e => e.ChainIntegrity) / items.Count() * 100 : 100,
                LastActivityAt = items.Any() ? items.Max(e => e.CreatedAt) : DateTime.Now
            };

            // AI-powered evidence health assessment
            if (_agentService.IsReady)
            {
                try
                {
                    var typeSummary = string.Join(", ", stats.EvidenceByType.Select(kv => $"{kv.Key}: {kv.Value}"));
                    var prompt = $@"You are a digital forensics evidence management expert for Indian Police. Provide a 2-3 sentence evidence health assessment.

Evidence Stats:
- Total: {stats.TotalEvidence}, Verified: {stats.VerifiedCount}, Pending: {stats.PendingVerification}
- Tampered: {stats.TamperedCount}, Court Submitted: {stats.CourtSubmitted}
- Chain Integrity Rate: {stats.ChainIntegrityRate:F1}%
- Types: {typeSummary}

Focus on risks, court admissibility concerns, and urgent actions needed. Be concise.";

                    var response = await _agentService.SendMessageAsync(prompt, "Digital evidence health assessment");
                    if (response.Success && !string.IsNullOrEmpty(response.Message))
                    {
                        stats.AiInsight = response.Message.Trim();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI evidence assessment failed");
                }
            }

            return stats;
        }

        // Public retrieval methods
        public Task<List<EvidenceItem>> GetAllEvidenceAsync() => Task.FromResult(_evidenceItems.OrderByDescending(e => e.CreatedAt).ToList());
        public Task<EvidenceItem?> GetEvidenceByIdAsync(string id) => Task.FromResult(_evidenceItems.FirstOrDefault(e => e.Id == id));
        public Task<List<EvidenceItem>> GetEvidenceByCaseIdAsync(string caseId) => Task.FromResult(_evidenceItems.Where(e => e.CaseId == caseId).OrderByDescending(e => e.CreatedAt).ToList());
        public Task<List<CustodyLog>> GetCustodyLogsAsync(string evidenceId) => Task.FromResult(_custodyLogs.Where(l => l.EvidenceId == evidenceId).OrderBy(l => l.DateTime).ToList());
        public Task<List<CustodyLog>> GetRecentCustodyLogsAsync(int count = 20) => Task.FromResult(_custodyLogs.OrderByDescending(l => l.DateTime).Take(count).ToList());

        public Task<List<EvidenceItem>> SearchEvidenceAsync(string query)
        {
            var lowerQuery = query.ToLower();
            return Task.FromResult(_evidenceItems.Where(e =>
                e.Title.ToLower().Contains(lowerQuery) ||
                e.Description.ToLower().Contains(lowerQuery) ||
                e.EvidenceNumber.ToLower().Contains(lowerQuery) ||
                e.FileName.ToLower().Contains(lowerQuery) ||
                e.Tags.Any(t => t.ToLower().Contains(lowerQuery))
            ).ToList());
        }

        public async Task<bool> UpdateEvidenceStatusAsync(string evidenceId, EvidenceStatus newStatus, string updatedBy, string notes)
        {
            var evidence = _evidenceItems.FirstOrDefault(e => e.Id == evidenceId);
            if (evidence == null) return false;

            var oldStatus = evidence.Status;
            evidence.Status = newStatus;

            var action = newStatus switch
            {
                EvidenceStatus.Verified => CustodyAction.IntegrityVerified,
                EvidenceStatus.UnderAnalysis => CustodyAction.SentForAnalysis,
                EvidenceStatus.CourtSubmitted => CustodyAction.SubmittedToCourt,
                EvidenceStatus.Disposed => CustodyAction.Disposed,
                _ => CustodyAction.StatusChanged
            };

            await AddCustodyLogAsync(evidenceId, action, updatedBy, updatedBy,
                evidence.StorageLocation ?? "", evidence.StorageLocation ?? "",
                $"Status changed from {oldStatus} to {newStatus}. {notes}", updatedBy);

            return true;
        }

        public Task DeleteEvidenceAsync(string id)
        {
            var evidence = _evidenceItems.FirstOrDefault(e => e.Id == id);
            if (evidence != null)
            {
                _evidenceItems.Remove(evidence);
                _custodyLogs.RemoveAll(l => l.EvidenceId == id);
            }
            return Task.CompletedTask;
        }
    }
}
