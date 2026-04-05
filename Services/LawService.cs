using AILegalAsst.Models;
using Microsoft.Extensions.Logging;

namespace AILegalAsst.Services;

public class LawService
{
    private readonly List<Law> _laws;
    private readonly AzureAgentService _agentService;
    private readonly ILogger<LawService> _logger;

    public LawService(AzureAgentService agentService, ILogger<LawService> logger)
    {
        _agentService = agentService;
        _logger = logger;
        _laws = InitializeLaws();
    }

    public List<Law> GetAllLaws()
    {
        return _laws;
    }

    public List<Law> GetLawsByType(LawType type)
    {
        return _laws.Where(l => l.Type == type).ToList();
    }

    public Law? GetLawById(int id)
    {
        return _laws.FirstOrDefault(l => l.Id == id);
    }

    public List<Law> SearchLaws(string searchTerm, LawType? type = null, bool? cybercrimeOnly = null)
    {
        // Try AI-powered semantic search first
        if (_agentService.IsReady && !string.IsNullOrWhiteSpace(searchTerm))
        {
            try
            {
                var allLawSummary = string.Join("\n", _laws.Select(l => $"ID:{l.Id}|{l.Title}|Sections:{string.Join(",", l.Sections.Select(s => s.SectionNumber + " " + s.Title))}|Keywords:{string.Join(",", l.Keywords)}"));
                var prompt = $"Given this Indian legal database:\n{allLawSummary}\n\nFind the most relevant laws and sections for: \"{searchTerm}\"\nReturn ONLY the Law IDs (numbers) that are relevant, comma-separated. Example: 1,3,5";
                var context = "You are an Indian legal search engine. Match user queries to relevant laws semantically. Return only numeric IDs.";

                var response = _agentService.SendMessageAsync(prompt, context).GetAwaiter().GetResult();
                if (response.Success && !string.IsNullOrWhiteSpace(response.Message))
                {
                    var ids = response.Message
                        .Split(new[] { ',', ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => int.TryParse(s, out _))
                        .Select(int.Parse)
                        .ToList();

                    if (ids.Count > 0)
                    {
                        var aiResults = _laws.Where(l => ids.Contains(l.Id)).ToList();
                        if (type.HasValue) aiResults = aiResults.Where(l => l.Type == type.Value).ToList();
                        if (cybercrimeOnly == true) aiResults = aiResults.Where(l => l.IsCybercrimeRelated).ToList();
                        if (aiResults.Count > 0) return aiResults;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI-powered law search failed, falling back to keyword search");
            }
        }

        // Fallback: keyword-based search
        var query = _laws.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(l =>
                l.Title.ToLower().Contains(term) ||
                l.Description.ToLower().Contains(term) ||
                l.Keywords.Any(k => k.ToLower().Contains(term)) ||
                l.Sections.Any(s => s.Title.ToLower().Contains(term) || s.Content.ToLower().Contains(term))
            );
        }

        if (type.HasValue)
        {
            query = query.Where(l => l.Type == type.Value);
        }

        if (cybercrimeOnly.HasValue && cybercrimeOnly.Value)
        {
            query = query.Where(l => l.IsCybercrimeRelated);
        }

        return query.ToList();
    }

    public List<LawSection> GetSectionsByLawId(int lawId)
    {
        var law = GetLawById(lawId);
        return law?.Sections ?? new List<LawSection>();
    }

    public LawSection? GetSection(int lawId, string sectionNumber)
    {
        var law = GetLawById(lawId);
        return law?.Sections.FirstOrDefault(s => s.SectionNumber == sectionNumber);
    }

    public List<Law> GetCybercrimeLaws()
    {
        return _laws.Where(l => l.IsCybercrimeRelated).ToList();
    }

    public List<Law> GetLawsForRole(string role)
    {
        // All roles can access all laws, but this method can be expanded for role-specific filtering
        return _laws;
    }

    private List<Law> InitializeLaws()
    {
        return new List<Law>
        {
            // Constitution of India
            new Law
            {
                Id = 1,
                Title = "Constitution of India",
                Type = LawType.Constitution,
                Year = 1950,
                Description = "The supreme law of India, laying down the framework for political principles, procedures, powers, and duties of government institutions.",
                EnactedDate = new DateTime(1950, 1, 26),
                LastAmended = new DateTime(2023, 8, 1),
                IsCybercrimeRelated = false,
                Keywords = new List<string> { "fundamental rights", "directive principles", "fundamental duties", "constitution" },
                Sections = new List<LawSection>
                {
                    new LawSection
                    {
                        Id = 1,
                        SectionNumber = "Article 14",
                        Title = "Equality before law",
                        Content = "The State shall not deny to any person equality before the law or the equal protection of the laws within the territory of India.",
                        Explanation = "This article guarantees equal treatment to all citizens and prohibits discrimination.",
                        IsBailable = false,
                        IsCognizable = false
                    },
                    new LawSection
                    {
                        Id = 2,
                        SectionNumber = "Article 19",
                        Title = "Protection of certain rights regarding freedom of speech, etc.",
                        Content = "All citizens shall have the right to freedom of speech and expression, to assemble peaceably and without arms, to form associations or unions, to move freely throughout the territory of India, to reside and settle in any part of the territory of India, and to practice any profession.",
                        Explanation = "This article guarantees six fundamental freedoms to all Indian citizens, subject to reasonable restrictions.",
                        IsBailable = false,
                        IsCognizable = false
                    },
                    new LawSection
                    {
                        Id = 3,
                        SectionNumber = "Article 21",
                        Title = "Protection of life and personal liberty",
                        Content = "No person shall be deprived of his life or personal liberty except according to procedure established by law.",
                        Explanation = "This is one of the most important fundamental rights, protecting life and liberty. Right to privacy is also derived from this article.",
                        IsBailable = false,
                        IsCognizable = false
                    },
                    new LawSection
                    {
                        Id = 4,
                        SectionNumber = "Article 32",
                        Title = "Remedies for enforcement of rights",
                        Content = "The right to move the Supreme Court by appropriate proceedings for the enforcement of the rights conferred by this Part is guaranteed.",
                        Explanation = "This article provides the right to constitutional remedies and is called the 'heart and soul' of the Constitution.",
                        IsBailable = false,
                        IsCognizable = false
                    }
                }
            },

            // Indian Penal Code
            new Law
            {
                Id = 2,
                Title = "Indian Penal Code (IPC)",
                Type = LawType.Act,
                ActNumber = "Act No. 45",
                Year = 1860,
                Description = "The main criminal code of India, defining crimes and providing punishments for various offences.",
                EnactedDate = new DateTime(1860, 10, 6),
                LastAmended = new DateTime(2023, 4, 1),
                IsCybercrimeRelated = false,
                Keywords = new List<string> { "criminal law", "penal code", "offences", "punishment", "crimes" },
                Sections = new List<LawSection>
                {
                    new LawSection
                    {
                        Id = 5,
                        SectionNumber = "Section 120B",
                        Title = "Punishment of criminal conspiracy",
                        Content = "Whoever is a party to a criminal conspiracy to commit an offence punishable with death, imprisonment for life or rigorous imprisonment for a term of two years or upwards, shall, where no express provision is made in this Code for the punishment of such a conspiracy, be punished in the manner provided in section 120A.",
                        Explanation = "Criminal conspiracy requires agreement between two or more persons to commit an illegal act.",
                        Punishment = "Imprisonment for a term not exceeding 6 months or fine or both for conspiracy to commit cognizable offence",
                        IsBailable = false,
                        IsCognizable = true
                    },
                    new LawSection
                    {
                        Id = 6,
                        SectionNumber = "Section 302",
                        Title = "Punishment for murder",
                        Content = "Whoever commits murder shall be punished with death, or imprisonment for life, and shall also be liable to fine.",
                        Explanation = "Murder is the most serious criminal offence, involving intentional killing with premeditation.",
                        Punishment = "Death or life imprisonment and fine",
                        IsBailable = false,
                        IsCognizable = true
                    },
                    new LawSection
                    {
                        Id = 7,
                        SectionNumber = "Section 354",
                        Title = "Assault or criminal force to woman with intent to outrage her modesty",
                        Content = "Whoever assaults or uses criminal force to any woman, intending to outrage or knowing it to be likely that he will thereby outrage her modesty, shall be punished with imprisonment of either description for a term which shall not be less than one year but which may extend to five years, and shall also be liable to fine.",
                        Explanation = "This section protects women from assault or use of criminal force intended to outrage their modesty.",
                        Punishment = "Imprisonment: 1-5 years and fine",
                        IsBailable = false,
                        IsCognizable = true
                    },
                    new LawSection
                    {
                        Id = 8,
                        SectionNumber = "Section 376",
                        Title = "Punishment for rape",
                        Content = "Whoever commits rape shall be punished with rigorous imprisonment of either description for a term which shall not be less than ten years, but which may extend to imprisonment for life, and shall also be liable to fine.",
                        Explanation = "Rape is a heinous crime with severe punishment. Special provisions exist for aggravated forms.",
                        Punishment = "Rigorous imprisonment: minimum 10 years to life imprisonment and fine",
                        IsBailable = false,
                        IsCognizable = true
                    },
                    new LawSection
                    {
                        Id = 9,
                        SectionNumber = "Section 405",
                        Title = "Criminal breach of trust",
                        Content = "Whoever, being in any manner entrusted with property, or with any dominion over property, dishonestly misappropriates or converts to his own use that property, or dishonestly uses or disposes of that property in violation of any direction of law prescribing the mode in which such trust is to be discharged, commits criminal breach of trust.",
                        Explanation = "Criminal breach of trust involves dishonest misappropriation of property entrusted to someone.",
                        Punishment = "Imprisonment up to 3 years or fine or both",
                        IsBailable = false,
                        IsCognizable = false
                    },
                    new LawSection
                    {
                        Id = 10,
                        SectionNumber = "Section 420",
                        Title = "Cheating and dishonestly inducing delivery of property",
                        Content = "Whoever cheats and thereby dishonestly induces the person deceived to deliver any property to any person, or to make, alter or destroy the whole or any part of a valuable security, or anything which is signed or sealed, shall be punished with imprisonment of either description for a term which may extend to seven years, and shall also be liable to fine.",
                        Explanation = "Section 420 deals with cheating and fraud, commonly invoked in financial crimes.",
                        Punishment = "Imprisonment up to 7 years and fine",
                        IsBailable = false,
                        IsCognizable = true
                    },
                    new LawSection
                    {
                        Id = 11,
                        SectionNumber = "Section 498A",
                        Title = "Husband or relative of husband of a woman subjecting her to cruelty",
                        Content = "Whoever, being the husband or the relative of the husband of a woman, subjects such woman to cruelty shall be punished with imprisonment for a term which may extend to three years and shall also be liable to fine.",
                        Explanation = "This section addresses domestic violence and cruelty towards married women by their husbands or in-laws.",
                        Punishment = "Imprisonment up to 3 years and fine",
                        IsBailable = false,
                        IsCognizable = true
                    }
                }
            },

            // Information Technology Act 2000
            new Law
            {
                Id = 3,
                Title = "Information Technology Act, 2000",
                Type = LawType.Act,
                ActNumber = "Act No. 21",
                Year = 2000,
                Description = "An act to provide legal recognition for transactions carried out by means of electronic data interchange and other means of electronic communication, and to prevent computer-based crimes.",
                EnactedDate = new DateTime(2000, 6, 9),
                LastAmended = new DateTime(2021, 2, 25),
                IsCybercrimeRelated = true,
                Keywords = new List<string> { "cybercrime", "IT act", "hacking", "data theft", "cyber law", "online fraud", "digital signature" },
                Sections = new List<LawSection>
                {
                    new LawSection
                    {
                        Id = 12,
                        SectionNumber = "Section 43",
                        Title = "Penalty for damage to computer, computer system, etc.",
                        Content = "If any person without permission of the owner or any other person who is in charge of a computer, computer system or computer network, accesses or secures access to such computer system or downloads data or introduces any computer virus, he shall be liable to pay damages by way of compensation to the person so affected.",
                        Explanation = "This section provides civil liability for unauthorized access and damage to computer systems.",
                        Punishment = "Compensation up to Rs. 1 crore to the affected person",
                        IsBailable = true,
                        IsCognizable = false
                    },
                    new LawSection
                    {
                        Id = 13,
                        SectionNumber = "Section 65",
                        Title = "Tampering with computer source documents",
                        Content = "Whoever knowingly or intentionally conceals, destroys or alters or intentionally or knowingly causes another to conceal, destroy or alter any computer source code used for a computer, computer programme, computer system or computer network, when the computer source code is required to be kept or maintained by law for the time being in force, shall be punishable with imprisonment up to three years, or with fine up to two lakh rupees, or with both.",
                        Explanation = "This section penalizes tampering with source code that is legally required to be maintained.",
                        Punishment = "Imprisonment up to 3 years or fine up to Rs. 2 lakhs or both",
                        IsBailable = true,
                        IsCognizable = true
                    },
                    new LawSection
                    {
                        Id = 14,
                        SectionNumber = "Section 66",
                        Title = "Computer related offences",
                        Content = "If any person, dishonestly or fraudulently, does any act referred to in section 43, he shall be punishable with imprisonment for a term which may extend to three years or with fine which may extend to five lakh rupees or with both.",
                        Explanation = "This section provides criminal liability for acts mentioned in Section 43 when done dishonestly or fraudulently.",
                        Punishment = "Imprisonment up to 3 years or fine up to Rs. 5 lakhs or both",
                        IsBailable = true,
                        IsCognizable = true
                    },
                    new LawSection
                    {
                        Id = 15,
                        SectionNumber = "Section 66B",
                        Title = "Punishment for dishonestly receiving stolen computer resource or communication device",
                        Content = "Whoever dishonestly receives or retains any stolen computer resource or communication device knowing or having reason to believe the same to be stolen computer resource or communication device, shall be punished with imprisonment of either description for a term which may extend to three years or with fine which may extend to rupees one lakh or with both.",
                        Explanation = "This section addresses receiving stolen digital property or devices.",
                        Punishment = "Imprisonment up to 3 years or fine up to Rs. 1 lakh or both",
                        IsBailable = true,
                        IsCognizable = true
                    },
                    new LawSection
                    {
                        Id = 16,
                        SectionNumber = "Section 66C",
                        Title = "Punishment for identity theft",
                        Content = "Whoever, fraudulently or dishonestly make use of the electronic signature, password or any other unique identification feature of any other person, shall be punished with imprisonment of either description for a term which may extend to three years and shall also be liable to fine which may extend to rupees one lakh.",
                        Explanation = "This section specifically deals with identity theft in the digital world.",
                        Punishment = "Imprisonment up to 3 years and fine up to Rs. 1 lakh",
                        IsBailable = true,
                        IsCognizable = true
                    },
                    new LawSection
                    {
                        Id = 17,
                        SectionNumber = "Section 66D",
                        Title = "Punishment for cheating by personation by using computer resource",
                        Content = "Whoever, by means of any communication device or computer resource cheats by personation, shall be punished with imprisonment of either description for a term which may extend to three years and shall also be liable to fine which may extend to one lakh rupees.",
                        Explanation = "This section covers online fraud through impersonation using digital means.",
                        Punishment = "Imprisonment up to 3 years and fine up to Rs. 1 lakh",
                        IsBailable = true,
                        IsCognizable = true
                    },
                    new LawSection
                    {
                        Id = 18,
                        SectionNumber = "Section 66E",
                        Title = "Punishment for violation of privacy",
                        Content = "Whoever, intentionally or knowingly captures, publishes or transmits the image of a private area of any person without his or her consent, under circumstances violating the privacy of that person, shall be punished with imprisonment which may extend to three years or with fine not exceeding two lakh rupees, or with both.",
                        Explanation = "This section protects privacy by penalizing unauthorized capture or publication of private images.",
                        Punishment = "Imprisonment up to 3 years or fine up to Rs. 2 lakhs or both",
                        IsBailable = true,
                        IsCognizable = true
                    },
                    new LawSection
                    {
                        Id = 19,
                        SectionNumber = "Section 66F",
                        Title = "Punishment for cyber terrorism",
                        Content = "Whoever, with intent to threaten the unity, integrity, security or sovereignty of India or to strike terror in the people or any section of the people by denying or cause the denial of access to any person authorized to access computer resource or attempting to penetrate or access a computer resource without authorization or exceeding authorized access, commits or conspires to commit cyber terrorism shall be punishable with imprisonment which may extend to imprisonment for life.",
                        Explanation = "This is the most serious cyber offence, dealing with cyber terrorism.",
                        Punishment = "Imprisonment up to life imprisonment",
                        IsBailable = false,
                        IsCognizable = true
                    },
                    new LawSection
                    {
                        Id = 20,
                        SectionNumber = "Section 67",
                        Title = "Punishment for publishing or transmitting obscene material in electronic form",
                        Content = "Whoever publishes or transmits or causes to be published or transmitted in the electronic form, any material which is lascivious or appeals to the prurient interest or if its effect is such as to tend to deprave and corrupt persons shall be punished on first conviction with imprisonment of either description for a term which may extend to three years and with fine which may extend to five lakh rupees.",
                        Explanation = "This section addresses publishing obscene content online.",
                        Punishment = "First conviction: Imprisonment up to 3 years and fine up to Rs. 5 lakhs; Subsequent conviction: Imprisonment up to 5 years and fine up to Rs. 10 lakhs",
                        IsBailable = true,
                        IsCognizable = true
                    },
                    new LawSection
                    {
                        Id = 21,
                        SectionNumber = "Section 67A",
                        Title = "Punishment for publishing or transmitting of material containing sexually explicit act, etc., in electronic form",
                        Content = "Whoever publishes or transmits or causes to be published or transmitted in the electronic form any material which contains sexually explicit act or conduct shall be punished on first conviction with imprisonment of either description for a term which may extend to five years and with fine which may extend to ten lakh rupees.",
                        Explanation = "This section specifically targets sexually explicit content online.",
                        Punishment = "First conviction: Imprisonment up to 5 years and fine up to Rs. 10 lakhs; Subsequent conviction: Imprisonment up to 7 years and fine up to Rs. 10 lakhs",
                        IsBailable = true,
                        IsCognizable = true
                    },
                    new LawSection
                    {
                        Id = 22,
                        SectionNumber = "Section 67B",
                        Title = "Punishment for publishing or transmitting of material depicting children in sexually explicit act, etc., in electronic form",
                        Content = "Whoever publishes or transmits or causes to be published or transmitted material in any electronic form which depicts children engaged in sexually explicit act or conduct shall be punished on first conviction with imprisonment of either description for a term which may extend to five years and with fine which may extend to ten lakh rupees.",
                        Explanation = "This section provides stringent punishment for child pornography and abuse material.",
                        Punishment = "First conviction: Imprisonment up to 5 years and fine up to Rs. 10 lakhs; Subsequent conviction: Imprisonment up to 7 years and fine up to Rs. 10 lakhs",
                        IsBailable = false,
                        IsCognizable = true
                    }
                }
            },

            // Code of Criminal Procedure
            new Law
            {
                Id = 4,
                Title = "Code of Criminal Procedure (CrPC)",
                Type = LawType.Act,
                ActNumber = "Act No. 2",
                Year = 1973,
                Description = "The primary legislation regarding the procedure for administration of criminal law in India. It provides machinery for investigation of crime, apprehension of suspects, collection of evidence, determination of guilt or innocence of the accused person and determination of punishment.",
                EnactedDate = new DateTime(1974, 4, 1),
                LastAmended = new DateTime(2023, 1, 1),
                IsCybercrimeRelated = false,
                Keywords = new List<string> { "criminal procedure", "investigation", "arrest", "bail", "trial", "FIR" },
                Sections = new List<LawSection>
                {
                    new LawSection
                    {
                        Id = 23,
                        SectionNumber = "Section 154",
                        Title = "Information in cognizable cases",
                        Content = "Every information relating to the commission of a cognizable offence, if given orally to an officer in charge of a police station, shall be reduced to writing by him or under his direction, and be read over to the informant; and every such information, whether given in writing or reduced to writing as aforesaid, shall be signed by the person giving it, and the substance thereof shall be entered in a book to be kept by such officer in such form as the State Government may prescribe in this behalf.",
                        Explanation = "This section deals with the First Information Report (FIR), the first step in criminal proceedings.",
                        IsBailable = false,
                        IsCognizable = false
                    },
                    new LawSection
                    {
                        Id = 24,
                        SectionNumber = "Section 41",
                        Title = "When police may arrest without warrant",
                        Content = "Any police officer may without an order from a Magistrate and without a warrant, arrest any person who has been concerned in any cognizable offence, or against whom a reasonable complaint has been made, or credible information has been received, or a reasonable suspicion exists, of his having been so concerned.",
                        Explanation = "This section provides police the power to arrest without warrant in cognizable offences.",
                        IsBailable = false,
                        IsCognizable = false
                    },
                    new LawSection
                    {
                        Id = 25,
                        SectionNumber = "Section 41A",
                        Title = "Notice of appearance before police officer",
                        Content = "The police officer may issue a notice directing the person against whom a reasonable suspicion exists that he has committed a cognizable offence to appear before him or at such other place as may be specified in the notice.",
                        Explanation = "This section allows police to summon a person for questioning instead of immediate arrest.",
                        IsBailable = false,
                        IsCognizable = false
                    },
                    new LawSection
                    {
                        Id = 26,
                        SectionNumber = "Section 50",
                        Title = "Person arrested to be informed of grounds of arrest and of right to bail",
                        Content = "Every police officer or other person arresting any person without warrant shall forthwith communicate to him full particulars of the offence for which he is arrested or other grounds for such arrest.",
                        Explanation = "This section protects the fundamental right of the arrested person to know the grounds of arrest.",
                        IsBailable = false,
                        IsCognizable = false
                    },
                    new LawSection
                    {
                        Id = 27,
                        SectionNumber = "Section 167",
                        Title = "Procedure when investigation cannot be completed in twenty-four hours",
                        Content = "Whenever any person is arrested and detained in custody, and it appears that the investigation cannot be completed within the period of twenty-four hours fixed by section 57, the officer in charge of the police station or the police officer making the investigation shall transmit to the nearest Judicial Magistrate a copy of the entries in the diary.",
                        Explanation = "This section deals with police custody and remand procedures.",
                        IsBailable = false,
                        IsCognizable = false
                    },
                    new LawSection
                    {
                        Id = 28,
                        SectionNumber = "Section 125",
                        Title = "Order for maintenance of wives, children and parents",
                        Content = "If any person having sufficient means neglects or refuses to maintain his wife unable to maintain herself, or his legitimate or illegitimate minor child unable to maintain itself, or his legitimate or illegitimate child who has attained majority, a Magistrate of the first class may, upon proof of such neglect or refusal, order such person to make a monthly allowance.",
                        Explanation = "This section provides for maintenance to wives, children, and parents.",
                        IsBailable = false,
                        IsCognizable = false
                    }
                }
            },

            // Indian Evidence Act
            new Law
            {
                Id = 5,
                Title = "Indian Evidence Act, 1872",
                Type = LawType.Act,
                ActNumber = "Act No. 1",
                Year = 1872,
                Description = "The law of evidence regulates the process of proof and the rules for admissibility of evidence in Indian courts.",
                EnactedDate = new DateTime(1872, 9, 1),
                LastAmended = new DateTime(2022, 6, 1),
                IsCybercrimeRelated = false,
                Keywords = new List<string> { "evidence", "proof", "witness", "document", "admissibility", "testimony" },
                Sections = new List<LawSection>
                {
                    new LawSection
                    {
                        Id = 29,
                        SectionNumber = "Section 3",
                        Title = "Interpretation clause",
                        Content = "Evidence means and includes all statements which the Court permits or requires to be made before it by witnesses, in relation to matters of fact under inquiry; such statements are called oral evidence; and all documents including electronic records produced for the inspection of the Court; such documents are called documentary evidence.",
                        Explanation = "This section defines what constitutes evidence in a court of law.",
                        IsBailable = false,
                        IsCognizable = false
                    },
                    new LawSection
                    {
                        Id = 30,
                        SectionNumber = "Section 45",
                        Title = "Opinions of experts",
                        Content = "When the Court has to form an opinion upon a point of foreign law, or of science, or art, or as to identity of handwriting or finger impressions, the opinions upon that point of persons specially skilled in such foreign law, science or art, or in questions as to identity of handwriting or finger impressions are relevant facts.",
                        Explanation = "This section allows expert opinions to be considered as evidence.",
                        IsBailable = false,
                        IsCognizable = false
                    },
                    new LawSection
                    {
                        Id = 31,
                        SectionNumber = "Section 65B",
                        Title = "Admissibility of electronic records",
                        Content = "Any information contained in an electronic record which is printed on a paper, stored, recorded or copied in optical or magnetic media produced by a computer shall be deemed to be also a document, if the conditions mentioned in this section are satisfied in relation to the information and computer in question and shall be admissible in any proceedings, without further proof or production of the original.",
                        Explanation = "This critical section deals with the admissibility of electronic evidence, crucial for cybercrime cases.",
                        IsBailable = false,
                        IsCognizable = false
                    },
                    new LawSection
                    {
                        Id = 32,
                        SectionNumber = "Section 106",
                        Title = "Burden of proving fact especially within knowledge",
                        Content = "When any fact is especially within the knowledge of any person, the burden of proving that fact is upon him.",
                        Explanation = "This section shifts the burden of proof to the person who has special knowledge of certain facts.",
                        IsBailable = false,
                        IsCognizable = false
                    }
                }
            }
        };
    }
}
