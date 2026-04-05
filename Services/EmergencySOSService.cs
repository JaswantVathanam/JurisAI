using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AILegalAsst.Models;

namespace AILegalAsst.Services
{
    /// <summary>
    /// Service for managing Legal Emergency SOS functionality with Azure Maps integration
    /// </summary>
    public class EmergencySOSService
    {
        private readonly List<SOSAlert> _alerts = new();
        private readonly List<SOSConfiguration> _configurations = new();
        private readonly List<Helpline> _helplines = new();
        private readonly Dictionary<EmergencyType, List<LegalRight>> _legalRights = new();
        private readonly LocationTrackingService? _locationService;
        private readonly ILogger<EmergencySOSService>? _logger;
        private readonly AzureAgentService _agentService;
        private int _nextAlertId = 1;

        public EmergencySOSService(
            AzureAgentService agentService,
            LocationTrackingService? locationService = null,
            ILogger<EmergencySOSService>? logger = null)
        {
            _agentService = agentService;
            _locationService = locationService;
            _logger = logger;
            InitializeHelplines();
            InitializeLegalRights();
        }

        /// <summary>
        /// Activate SOS alert with location tracking
        /// </summary>
        public async Task<SOSAlert> ActivateSOSAsync(
            string userId,
            string userName,
            string userPhone,
            EmergencyType emergencyType,
            double? latitude = null,
            double? longitude = null)
        {
            var alert = new SOSAlert
            {
                Id = _nextAlertId++,
                UserId = userId,
                UserName = userName,
                UserPhone = userPhone,
                EmergencyType = emergencyType,
                ActivatedAt = DateTime.Now,
                Status = SOSStatus.Active,
                Latitude = latitude,
                Longitude = longitude,
                IsRecording = true
            };

            // Track location if coordinates provided
            if (latitude.HasValue && longitude.HasValue && _locationService != null)
            {
                try
                {
                    var location = await _locationService.TrackLocationAsync(
                        latitude.Value,
                        longitude.Value,
                        "SOS",
                        alert.Id.ToString(),
                        userName,
                        $"Emergency SOS - {emergencyType}");

                    // Store location address in notes field if available
                    _logger?.LogInformation(
                        "[EmergencySOS] Location tracked for SOS {Id}: {Address}",
                        alert.Id, location.Address);

                    // Find nearby emergency services
                    var nearbyServices = await _locationService.FindNearbyEmergencyServicesAsync(
                        latitude.Value, longitude.Value, 5000);

                    // Log nearby services
                    var policeCount = nearbyServices.ContainsKey("PoliceStations") 
                        ? nearbyServices["PoliceStations"].Count : 0;
                    var hospitalCount = nearbyServices.ContainsKey("Hospitals")
                        ? nearbyServices["Hospitals"].Count : 0;

                    _logger?.LogInformation(
                        "[EmergencySOS] Found {Police} police stations, {Hospitals} hospitals near SOS location",
                        policeCount, hospitalCount);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "[EmergencySOS] Error tracking location for SOS {Id}", alert.Id);
                }
            }

            // Get user configuration
            var config = GetUserConfiguration(userId);

            // Notify emergency contacts
            if (config?.EmergencyContacts.Any() == true)
            {
                foreach (var contact in config.EmergencyContacts.Where(c => c.NotifyOnSOS))
                {
                    contact.LastNotified = DateTime.Now;
                    alert.NotifiedContacts.Add(contact);
                }
            }

            // For detention cases, notify NHRC
            if (emergencyType == EmergencyType.WrongfulDetention || 
                emergencyType == EmergencyType.PoliceHarassment)
            {
                if (config?.NotifyNHRCOnDetention == true)
                {
                    alert.NHRCNotified = true;
                }
            }

            // Notify nearby lawyers (simulated)
            alert.NotifiedLawyers = GetNearbyLawyers(latitude, longitude);

            _alerts.Add(alert);
            
            return alert;
        }

        /// <summary>
        /// Deactivate SOS - user marked safe
        /// </summary>
        public async Task<SOSAlert?> DeactivateSOSAsync(int alertId, string userId, string? notes = null)
        {
            var alert = _alerts.FirstOrDefault(a => a.Id == alertId && a.UserId == userId);
            if (alert == null) return null;

            alert.DeactivatedAt = DateTime.Now;
            alert.Status = SOSStatus.UserMarkedSafe;
            alert.UserMarkedSafe = true;
            alert.ResolutionNotes = notes;
            alert.IsRecording = false;

            await Task.CompletedTask;
            return alert;
        }

        /// <summary>
        /// Get active SOS for user
        /// </summary>
        public SOSAlert? GetActiveAlert(string userId)
        {
            return _alerts.FirstOrDefault(a => 
                a.UserId == userId && 
                a.Status == SOSStatus.Active);
        }

        /// <summary>
        /// Get SOS history for user
        /// </summary>
        public SOSHistory GetHistory(string userId)
        {
            var userAlerts = _alerts.Where(a => a.UserId == userId).OrderByDescending(a => a.ActivatedAt).ToList();
            
            return new SOSHistory
            {
                Alerts = userAlerts,
                TotalAlerts = userAlerts.Count,
                ResolvedAlerts = userAlerts.Count(a => a.Status != SOSStatus.Active),
                LastAlertDate = userAlerts.FirstOrDefault()?.ActivatedAt
            };
        }

        /// <summary>
        /// Get user SOS configuration
        /// </summary>
        public SOSConfiguration? GetUserConfiguration(string userId)
        {
            return _configurations.FirstOrDefault(c => c.UserId == userId);
        }

        /// <summary>
        /// Save user SOS configuration
        /// </summary>
        public async Task SaveConfigurationAsync(SOSConfiguration config)
        {
            var existing = _configurations.FirstOrDefault(c => c.UserId == config.UserId);
            if (existing != null)
            {
                _configurations.Remove(existing);
            }
            _configurations.Add(config);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Get legal rights based on emergency type
        /// </summary>
        public List<LegalRight> GetLegalRights(EmergencyType emergencyType)
        {
            return _legalRights.TryGetValue(emergencyType, out var rights) ? rights : new List<LegalRight>();
        }

        /// <summary>
        /// Get relevant helplines based on emergency type
        /// </summary>
        public List<Helpline> GetHelplines(EmergencyType? emergencyType = null)
        {
            if (emergencyType == null)
                return _helplines;

            return _helplines.Where(h => h.ApplicableFor.Contains(emergencyType.Value) || h.IsNational).ToList();
        }

        /// <summary>
        /// Get emergency type display name
        /// </summary>
        public string GetEmergencyTypeName(EmergencyType type)
        {
            return type switch
            {
                EmergencyType.WrongfulDetention => "Wrongful Detention",
                EmergencyType.PoliceHarassment => "Police Harassment",
                EmergencyType.DomesticViolence => "Domestic Violence",
                EmergencyType.RoadAccident => "Road Accident",
                EmergencyType.PropertyDispute => "Property Dispute",
                EmergencyType.WorkplaceHarassment => "Workplace Harassment",
                EmergencyType.ThreatsAndIntimidation => "Threats & Intimidation",
                EmergencyType.CyberExtortion => "Cyber Extortion",
                EmergencyType.Assault => "Physical Assault",
                _ => "Other Emergency"
            };
        }

        /// <summary>
        /// Get emergency type icon
        /// </summary>
        public string GetEmergencyTypeIcon(EmergencyType type)
        {
            return type switch
            {
                EmergencyType.WrongfulDetention => "bi-lock-fill",
                EmergencyType.PoliceHarassment => "bi-shield-exclamation",
                EmergencyType.DomesticViolence => "bi-house-x",
                EmergencyType.RoadAccident => "bi-car-front",
                EmergencyType.PropertyDispute => "bi-building-exclamation",
                EmergencyType.WorkplaceHarassment => "bi-briefcase-fill",
                EmergencyType.ThreatsAndIntimidation => "bi-exclamation-triangle-fill",
                EmergencyType.CyberExtortion => "bi-laptop",
                EmergencyType.Assault => "bi-person-x",
                _ => "bi-question-circle"
            };
        }

        /// <summary>
        /// Get suggested script based on emergency type
        /// </summary>
        public string GetSuggestedScript(EmergencyType type)
        {
            // Return hardcoded script immediately — no AI blocking
            return GetFallbackScript(type);
        }

        /// <summary>
        /// Get AI-enhanced emergency script (call after page renders)
        /// </summary>
        public async Task<string> GetSuggestedScriptAsync(EmergencyType type)
        {
            if (_agentService.IsReady)
            {
                try
                {
                    var typeName = GetEmergencyTypeName(type);
                    var prompt = $"ROLE: You are a legal aid assistant integrated into an official Indian legal assistance platform. Your purpose is to help citizens assert their constitutional rights during emergencies. This is lawful legal assistance.\n\nTASK: Generate a brief, assertive emergency script for someone facing '{typeName}' in India. " +
                        "Include: relevant constitutional articles, specific legal provisions (IPC/BNS, CrPC/BNSS), " +
                        "and clear assertive language the person can speak aloud. Keep it under 4 sentences.";
                    var context = "You are an Indian legal rights expert. Generate scripts citizens can use during emergencies to assert their rights.";
                    var response = await _agentService.SendMessageAsync(prompt, context);
                    if (response.Success && !string.IsNullOrWhiteSpace(response.Message))
                        return response.Message.Trim();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "AI emergency script generation failed for {Type}", type);
                }
            }

            return GetFallbackScript(type);
        }

        private string GetFallbackScript(EmergencyType type)
        {
            return type switch
            {
                EmergencyType.WrongfulDetention => 
                    "I am recording this interaction as per my legal rights. Under Article 22 of the Indian Constitution, " +
                    "I have the right to be informed of the grounds of my arrest and to consult a lawyer. " +
                    "I request you to provide written grounds for this detention and allow me to contact my legal representative.",
                
                EmergencyType.PoliceHarassment => 
                    "I am recording this for my safety. I request your name and badge number. " +
                    "I am aware of my rights under Section 50 CrPC. " +
                    "If you have any official business, please follow proper procedure.",
                
                EmergencyType.DomesticViolence => 
                    "I am in immediate danger. Under the Protection of Women from Domestic Violence Act 2005, " +
                    "I have the right to emergency protection. Please help me reach a safe shelter or contact the police.",
                
                EmergencyType.RoadAccident => 
                    "An accident has occurred. I am documenting the scene. " +
                    "Under Motor Vehicles Act, all parties must exchange insurance and license details.",
                
                _ => "I am recording this interaction for legal documentation purposes. " +
                     "I am aware of my constitutional rights and request that proper legal procedure be followed."
            };
        }

        #region Private Methods

        private List<LawyerAlert> GetNearbyLawyers(double? latitude, double? longitude)
        {
            // Simulated nearby lawyers
            return new List<LawyerAlert>
            {
                new LawyerAlert
                {
                    LawyerId = "lawyer_1",
                    LawyerName = "Adv. Priya Mehta",
                    Phone = "9876543210",
                    DistanceKm = 2.3,
                    NotifiedAt = DateTime.Now,
                    Responded = false
                },
                new LawyerAlert
                {
                    LawyerId = "lawyer_2",
                    LawyerName = "Adv. Rajesh Kumar",
                    Phone = "9876543211",
                    DistanceKm = 4.1,
                    NotifiedAt = DateTime.Now,
                    Responded = false
                },
                new LawyerAlert
                {
                    LawyerId = "lawyer_3",
                    LawyerName = "Adv. Sunita Rao",
                    Phone = "9876543212",
                    DistanceKm = 5.8,
                    NotifiedAt = DateTime.Now,
                    Responded = false
                }
            };
        }

        private void InitializeHelplines()
        {
            _helplines.AddRange(new[]
            {
                new Helpline
                {
                    Name = "Police Emergency",
                    Number = "112",
                    Description = "National Emergency Response",
                    Icon = "bi-shield-fill",
                    IsNational = true,
                    ApplicableFor = Enum.GetValues<EmergencyType>().ToList()
                },
                new Helpline
                {
                    Name = "Women Helpline",
                    Number = "181",
                    Description = "Women in distress - 24x7",
                    Icon = "bi-person-heart",
                    IsNational = true,
                    ApplicableFor = new List<EmergencyType> 
                    { 
                        EmergencyType.DomesticViolence, 
                        EmergencyType.WorkplaceHarassment,
                        EmergencyType.Assault
                    }
                },
                new Helpline
                {
                    Name = "Cyber Crime",
                    Number = "1930",
                    Description = "National Cyber Crime Helpline",
                    Icon = "bi-laptop",
                    IsNational = true,
                    ApplicableFor = new List<EmergencyType> { EmergencyType.CyberExtortion }
                },
                new Helpline
                {
                    Name = "Road Accident",
                    Number = "1073",
                    Description = "Road Accident Emergency",
                    Icon = "bi-car-front-fill",
                    IsNational = true,
                    ApplicableFor = new List<EmergencyType> { EmergencyType.RoadAccident }
                },
                new Helpline
                {
                    Name = "Human Rights",
                    Number = "14433",
                    Description = "NHRC - National Human Rights Commission",
                    Icon = "bi-person-check",
                    IsNational = true,
                    ApplicableFor = new List<EmergencyType> 
                    { 
                        EmergencyType.WrongfulDetention, 
                        EmergencyType.PoliceHarassment 
                    }
                },
                new Helpline
                {
                    Name = "Legal Aid",
                    Number = "15100",
                    Description = "NALSA - Free Legal Aid",
                    Icon = "bi-briefcase-fill",
                    IsNational = true,
                    ApplicableFor = Enum.GetValues<EmergencyType>().ToList()
                },
                new Helpline
                {
                    Name = "Senior Citizen",
                    Number = "14567",
                    Description = "Elder Helpline",
                    Icon = "bi-person-walking",
                    IsNational = true,
                    ApplicableFor = new List<EmergencyType> 
                    { 
                        EmergencyType.PropertyDispute, 
                        EmergencyType.ThreatsAndIntimidation 
                    }
                },
                new Helpline
                {
                    Name = "Child Helpline",
                    Number = "1098",
                    Description = "CHILDLINE - Child Protection",
                    Icon = "bi-emoji-smile",
                    IsNational = true,
                    ApplicableFor = new List<EmergencyType> 
                    { 
                        EmergencyType.DomesticViolence, 
                        EmergencyType.Assault 
                    }
                }
            });
        }

        private void InitializeLegalRights()
        {
            // Wrongful Detention Rights
            _legalRights[EmergencyType.WrongfulDetention] = new List<LegalRight>
            {
                new LegalRight
                {
                    Article = "Article 22(1)",
                    Title = "Right to be Informed",
                    Description = "Every person arrested shall be informed of the grounds of arrest as soon as possible.",
                    Script = "Please inform me of the grounds of my arrest in writing."
                },
                new LegalRight
                {
                    Article = "Article 22(1)",
                    Title = "Right to Legal Counsel",
                    Description = "You have the right to consult and be defended by a legal practitioner of your choice.",
                    Script = "I wish to speak with my lawyer before any questioning."
                },
                new LegalRight
                {
                    Article = "Article 22(2)",
                    Title = "Right to Magistrate",
                    Description = "You must be produced before a magistrate within 24 hours of arrest.",
                    Script = "I must be produced before a magistrate within 24 hours."
                },
                new LegalRight
                {
                    Article = "Section 41D CrPC",
                    Title = "Right to Inform",
                    Description = "You have the right to have a relative or friend informed of your arrest.",
                    Script = "I request you to inform my family member about this arrest."
                },
                new LegalRight
                {
                    Article = "Section 50 CrPC",
                    Title = "Right to Know Grounds",
                    Description = "Person arrested must be informed of full particulars of the offence.",
                    Script = "Under Section 50, please provide full particulars of the alleged offence."
                },
                new LegalRight
                {
                    Article = "D.K. Basu Guidelines",
                    Title = "Arrest Memo",
                    Description = "An arrest memo must be prepared with date, time, and witness signature.",
                    Script = "Please prepare an arrest memo as per D.K. Basu guidelines."
                }
            };

            // Police Harassment Rights
            _legalRights[EmergencyType.PoliceHarassment] = new List<LegalRight>
            {
                new LegalRight
                {
                    Article = "Section 166A IPC",
                    Title = "Protection from Harassment",
                    Description = "Public servants can be prosecuted for not recording FIR or preventing investigation.",
                    Script = "I am aware of Section 166A IPC regarding police accountability."
                },
                new LegalRight
                {
                    Article = "Article 21",
                    Title = "Right to Life & Liberty",
                    Description = "No person shall be deprived of life or personal liberty except by procedure established by law.",
                    Script = "My right to liberty under Article 21 is being violated."
                },
                new LegalRight
                {
                    Article = "Section 197 CrPC",
                    Title = "Complaint Against Officer",
                    Description = "You can file a complaint with the SP/DGP against the officer.",
                    Script = "I will be filing a formal complaint with your senior officer."
                }
            };

            // Domestic Violence Rights
            _legalRights[EmergencyType.DomesticViolence] = new List<LegalRight>
            {
                new LegalRight
                {
                    Article = "Section 12 PWDVA",
                    Title = "Protection Order",
                    Description = "You can obtain a protection order from Magistrate against the abuser.",
                    Script = "I need emergency protection under the DV Act 2005."
                },
                new LegalRight
                {
                    Article = "Section 17 PWDVA",
                    Title = "Right to Residence",
                    Description = "You have the right to reside in the shared household regardless of ownership.",
                    Script = "I have a legal right to reside in this house."
                },
                new LegalRight
                {
                    Article = "Section 498A IPC",
                    Title = "Cruelty by Husband",
                    Description = "Cruelty by husband or relatives is a criminal offence.",
                    Script = "This constitutes cruelty under Section 498A IPC."
                },
                new LegalRight
                {
                    Article = "Section 9 PWDVA",
                    Title = "Protection Officers",
                    Description = "You can contact Protection Officers appointed by the state.",
                    Script = "I need to speak with a Protection Officer immediately."
                }
            };

            // Road Accident Rights
            _legalRights[EmergencyType.RoadAccident] = new List<LegalRight>
            {
                new LegalRight
                {
                    Article = "Section 134 MVA",
                    Title = "Duty to Report",
                    Description = "Driver must report accident to police within 24 hours.",
                    Script = "Please provide your license and insurance details for the FIR."
                },
                new LegalRight
                {
                    Article = "Section 161 MVA",
                    Title = "Hit & Run Compensation",
                    Description = "Victims of hit and run are entitled to compensation from govt fund.",
                    Script = "I am entitled to compensation under the Hit & Run scheme."
                },
                new LegalRight
                {
                    Article = "Good Samaritan Law",
                    Title = "Bystander Protection",
                    Description = "Good Samaritans helping accident victims are protected from harassment.",
                    Script = "Under Good Samaritan Law, helpers cannot be harassed."
                }
            };

            // Workplace Harassment
            _legalRights[EmergencyType.WorkplaceHarassment] = new List<LegalRight>
            {
                new LegalRight
                {
                    Article = "POSH Act 2013",
                    Title = "ICC Complaint",
                    Description = "Every organization must have Internal Complaints Committee.",
                    Script = "I want to file a complaint with the Internal Complaints Committee."
                },
                new LegalRight
                {
                    Article = "Section 354A IPC",
                    Title = "Sexual Harassment",
                    Description = "Sexual harassment is a criminal offence punishable with imprisonment.",
                    Script = "This constitutes sexual harassment under Section 354A IPC."
                },
                new LegalRight
                {
                    Article = "Section 509 IPC",
                    Title = "Outraging Modesty",
                    Description = "Word, gesture or act intended to insult modesty is punishable.",
                    Script = "This behaviour violates Section 509 IPC."
                }
            };

            // Generic rights for other types
            var genericRights = new List<LegalRight>
            {
                new LegalRight
                {
                    Article = "Article 21",
                    Title = "Right to Life",
                    Description = "Protection of life and personal liberty.",
                    Script = "I invoke my fundamental right to life and liberty."
                },
                new LegalRight
                {
                    Article = "First Information Report",
                    Title = "Right to Lodge FIR",
                    Description = "Police must register FIR for cognizable offences. Zero FIR can be filed at any station.",
                    Script = "I want to lodge an FIR for this incident."
                }
            };

            _legalRights[EmergencyType.PropertyDispute] = genericRights;
            _legalRights[EmergencyType.ThreatsAndIntimidation] = genericRights;
            _legalRights[EmergencyType.CyberExtortion] = genericRights;
            _legalRights[EmergencyType.Assault] = genericRights;
            _legalRights[EmergencyType.Other] = genericRights;
        }

        #endregion
    }
}
