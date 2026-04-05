using System;
using System.Collections.Generic;

namespace AILegalAsst.Models
{
    /// <summary>
    /// Represents an SOS emergency alert
    /// </summary>
    public class SOSAlert
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserPhone { get; set; } = string.Empty;
        public EmergencyType EmergencyType { get; set; }
        public DateTime ActivatedAt { get; set; } = DateTime.Now;
        public DateTime? DeactivatedAt { get; set; }
        public SOSStatus Status { get; set; } = SOSStatus.Active;
        
        // Location
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        
        // Recording
        public bool IsRecording { get; set; }
        public string? RecordingUrl { get; set; }
        public int RecordingDurationSeconds { get; set; }
        
        // Notifications
        public List<EmergencyContact> NotifiedContacts { get; set; } = new();
        public List<LawyerAlert> NotifiedLawyers { get; set; } = new();
        public bool NHRCNotified { get; set; }
        public bool PoliceNotified { get; set; }
        
        // Resolution
        public string? ResolutionNotes { get; set; }
        public bool UserMarkedSafe { get; set; }
    }

    /// <summary>
    /// Emergency contact for SOS alerts
    /// </summary>
    public class EmergencyContact
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Relationship { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public bool NotifyOnSOS { get; set; } = true;
        public DateTime? LastNotified { get; set; }
    }

    /// <summary>
    /// Lawyer alert for SOS
    /// </summary>
    public class LawyerAlert
    {
        public int Id { get; set; }
        public string LawyerId { get; set; } = string.Empty;
        public string LawyerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
        public DateTime NotifiedAt { get; set; }
        public bool Responded { get; set; }
        public string? Response { get; set; }
    }

    /// <summary>
    /// Legal rights based on emergency type
    /// </summary>
    public class LegalRight
    {
        public string Article { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Script { get; set; } = string.Empty; // What to say
    }

    /// <summary>
    /// Helpline information
    /// </summary>
    public class Helpline
    {
        public string Name { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsNational { get; set; }
        public List<EmergencyType> ApplicableFor { get; set; } = new();
    }

    /// <summary>
    /// SOS configuration for a user
    /// </summary>
    public class SOSConfiguration
    {
        public string UserId { get; set; } = string.Empty;
        public List<EmergencyContact> EmergencyContacts { get; set; } = new();
        public bool AutoRecordOnSOS { get; set; } = true;
        public bool ShareLocationOnSOS { get; set; } = true;
        public bool NotifyLawyersOnSOS { get; set; } = true;
        public bool NotifyNHRCOnDetention { get; set; } = true;
        public string? PreferredLanguage { get; set; } = "en";
        public string? MedicalInfo { get; set; } // Allergies, conditions
        public string? BloodGroup { get; set; }
    }

    /// <summary>
    /// SOS History for a user
    /// </summary>
    public class SOSHistory
    {
        public List<SOSAlert> Alerts { get; set; } = new();
        public int TotalAlerts { get; set; }
        public int ResolvedAlerts { get; set; }
        public DateTime? LastAlertDate { get; set; }
    }

    public enum EmergencyType
    {
        WrongfulDetention,
        PoliceHarassment,
        DomesticViolence,
        RoadAccident,
        PropertyDispute,
        WorkplaceHarassment,
        ThreatsAndIntimidation,
        CyberExtortion,
        Assault,
        Other
    }

    public enum SOSStatus
    {
        Active,
        LawyerResponded,
        PoliceInformed,
        UserMarkedSafe,
        Resolved,
        Cancelled
    }
}
