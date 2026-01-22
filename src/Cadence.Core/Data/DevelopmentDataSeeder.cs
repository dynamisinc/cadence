using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Data;

/// <summary>
/// Seeds development/demo data on application startup.
/// Only runs in Development environment and is idempotent.
///
/// Creates realistic emergency management exercise data including:
/// - Organization and multiple users with different roles
/// - Hurricane TTX (Active) with full MSEL, phases, objectives, and varied inject statuses
/// - Active Threat FSE (Draft) without MSEL
/// - Flood Response Training (Practice Mode) for testing practice mode features
/// </summary>
public static class DevelopmentDataSeeder
{
    #region Fixed GUIDs for Idempotent Seeding

    // Organization
    private static readonly Guid DemoOrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // Users
    // ApplicationUser IDs are strings (ASP.NET Core Identity)
    private static readonly string AdminUserId = "22222222-2222-2222-2222-222222222222";
    private static readonly string DirectorUserId = "22222222-2222-2222-2222-222222222233";
    private static readonly string Controller1UserId = "22222222-2222-2222-2222-222222222244";
    private static readonly string Controller2UserId = "22222222-2222-2222-2222-222222222255";
    private static readonly string EvaluatorUserId = "22222222-2222-2222-2222-222222222266";
    private static readonly string ObserverUserId = "22222222-2222-2222-2222-222222222277";

    // Exercises
    private static readonly Guid HurricaneTtxId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ActiveShooterFseId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid FloodTrainingId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    // MSELs
    private static readonly Guid HurricaneMselId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid FloodMselId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    // Phases (fixed for referencing)
    private static readonly Guid Phase1Id = Guid.Parse("88888888-8888-8888-8888-888888888801");
    private static readonly Guid Phase2Id = Guid.Parse("88888888-8888-8888-8888-888888888802");
    private static readonly Guid Phase3Id = Guid.Parse("88888888-8888-8888-8888-888888888803");

    // Objectives (fixed for referencing)
    private static readonly Guid Objective1Id = Guid.Parse("99999999-9999-9999-9999-999999999901");
    private static readonly Guid Objective2Id = Guid.Parse("99999999-9999-9999-9999-999999999902");
    private static readonly Guid Objective3Id = Guid.Parse("99999999-9999-9999-9999-999999999903");
    private static readonly Guid Objective4Id = Guid.Parse("99999999-9999-9999-9999-999999999904");

    #endregion

    /// <summary>
    /// Seeds demo data if not already present. Idempotent - safe to call multiple times.
    /// </summary>
    public static async Task SeedAsync(AppDbContext context)
    {
        // Check if already seeded (organization exists)
        if (await context.Organizations.AnyAsync(o => o.Id == DemoOrgId))
        {
            return; // Already seeded
        }

        var now = DateTime.UtcNow;

        // 1. Create Demo Organization
        var demoOrg = CreateOrganization();
        context.Organizations.Add(demoOrg);

        // 2. TODO: Create Demo ApplicationUsers via UserManager
        // NOTE: The deprecated User table is no longer seeded.
        // ApplicationUsers should be created through the authentication system or via UserManager.
        // For development, register users through the /auth/register endpoint or use a separate seeding script.
        // var users = CreateUsers();
        // context.Users.AddRange(users);

        // 3. Create Exercises (without ActiveMselId to avoid circular dependency)
        var hurricaneTtx = CreateHurricaneTtxExercise(now);
        var activeShooterFse = CreateActiveShooterExercise(now);
        var floodTraining = CreateFloodTrainingExercise(now);

        context.Exercises.Add(hurricaneTtx);
        context.Exercises.Add(activeShooterFse);
        context.Exercises.Add(floodTraining);

        // Save exercises first
        await context.SaveChangesAsync();

        // 4. Create MSELs (now that Exercises exist)
        var hurricaneMsel = CreateHurricaneMsel();
        var floodMsel = CreateFloodMsel();

        context.Msels.Add(hurricaneMsel);
        context.Msels.Add(floodMsel);

        // 5. Create Phases
        var hurricanePhases = CreateHurricanePhases();
        context.Phases.AddRange(hurricanePhases);

        // Save MSELs and Phases
        await context.SaveChangesAsync();

        // 6. Set ActiveMselId on Exercises (no circular dependency now)
        hurricaneTtx.ActiveMselId = HurricaneMselId;
        floodTraining.ActiveMselId = FloodMselId;

        // 7. Create Objectives (for Hurricane TTX)
        var objectives = CreateHurricaneObjectives();
        context.Objectives.AddRange(objectives);

        // 8. Create Injects
        var hurricaneInjects = CreateHurricaneInjects(now);
        var floodInjects = CreateFloodInjects();

        context.Injects.AddRange(hurricaneInjects);
        context.Injects.AddRange(floodInjects);

        // 9. Create Participant Assignments
        // TODO: Seed ExerciseParticipants once ApplicationUsers are seeded
        // var hurricaneParticipants = CreateHurricaneParticipants(now);
        // var floodParticipants = CreateFloodParticipants(now);
        // context.ExerciseParticipants.AddRange(hurricaneParticipants);
        // context.ExerciseParticipants.AddRange(floodParticipants);

        await context.SaveChangesAsync();
    }

    #region Organization

    private static Organization CreateOrganization()
    {
        return new Organization
        {
            Id = DemoOrgId,
            Name = "Metro County Emergency Management Agency",
            Description = "County-level emergency management agency responsible for coordinating disaster preparedness, " +
                          "response, and recovery operations for Metro County and its 12 municipalities.",
            CreatedBy = Guid.Empty,
            ModifiedBy = Guid.Empty
        };
    }

    #endregion

    #region Users (DEPRECATED - Use ApplicationUsers via UserManager)

    // TODO: Replace with ApplicationUser seeding via UserManager
    // The deprecated User table is no longer used. ApplicationUsers are created through:
    // 1. Authentication endpoints (/auth/register)
    // 2. Direct UserManager usage with proper password hashing
    // 3. Separate admin seeding script
    //
    // For reference, these were the demo users:
    // - admin@metrocounty.gov (Maria Chen) - Admin
    // - jwashington@metrocounty.gov (James Washington) - Manager/Director
    // - smartinez@metrocounty.gov (Sarah Martinez) - User/Controller
    // - mbrown@metrocounty.gov (Michael Brown) - User/Controller
    // - ldavis@metrocounty.gov (Lisa Davis) - User/Evaluator
    // - rjohnson@metrocounty.gov (Robert Johnson) - User/Observer

    /*
    private static List<User> CreateUsers()
    {
        return new List<User>
        {
            new User
            {
                Id = AdminUserId,
                Email = "admin@metrocounty.gov",
                DisplayName = "Maria Chen",
                IsSystemAdmin = true,
                OrganizationId = DemoOrgId,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },
            new User
            {
                Id = DirectorUserId,
                Email = "jwashington@metrocounty.gov",
                DisplayName = "James Washington",
                IsSystemAdmin = false,
                OrganizationId = DemoOrgId,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },
            new User
            {
                Id = Controller1UserId,
                Email = "smartinez@metrocounty.gov",
                DisplayName = "Sarah Martinez",
                IsSystemAdmin = false,
                OrganizationId = DemoOrgId,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },
            new User
            {
                Id = Controller2UserId,
                Email = "mbrown@metrocounty.gov",
                DisplayName = "Michael Brown",
                IsSystemAdmin = false,
                OrganizationId = DemoOrgId,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },
            new User
            {
                Id = EvaluatorUserId,
                Email = "ldavis@metrocounty.gov",
                DisplayName = "Lisa Davis",
                IsSystemAdmin = false,
                OrganizationId = DemoOrgId,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },
            new User
            {
                Id = ObserverUserId,
                Email = "rjohnson@metrocounty.gov",
                DisplayName = "Robert Johnson",
                IsSystemAdmin = false,
                OrganizationId = DemoOrgId,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            }
        };
    }
    */

    #endregion

    #region Exercises

    private static Exercise CreateHurricaneTtxExercise(DateTime now)
    {
        return new Exercise
        {
            Id = HurricaneTtxId,
            Name = "Hurricane Response TTX 2026",
            Description = "Annual tabletop exercise focusing on hurricane evacuation and shelter operations. " +
                          "This exercise will test coordination between EOC, public works, shelter management, " +
                          "and mutual aid partners. Scenario involves a Category 3 hurricane making landfall " +
                          "with significant storm surge and inland flooding impacts.",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Active,
            IsPracticeMode = false,
            ScheduledDate = DateOnly.FromDateTime(now.AddDays(14)),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0),
            TimeZoneId = "America/New_York",
            Location = "Metro County EOC, Conference Room A",
            OrganizationId = DemoOrgId,
            CreatedBy = Guid.Empty, // TODO: Use actual user ID once ApplicationUsers are seeded
            ModifiedBy = Guid.Empty
        };
    }

    private static Exercise CreateActiveShooterExercise(DateTime now)
    {
        return new Exercise
        {
            Id = ActiveShooterFseId,
            Name = "Active Threat Response FSE",
            Description = "Full-scale exercise testing law enforcement, fire/EMS, and hospital coordination " +
                          "for active threat incidents at Metro County Courthouse. Exercise will include " +
                          "simulated patients, unified command establishment, and family reunification.",
            ExerciseType = ExerciseType.FSE,
            Status = ExerciseStatus.Draft,
            IsPracticeMode = false,
            ScheduledDate = DateOnly.FromDateTime(now.AddMonths(2)),
            StartTime = new TimeOnly(18, 0),
            EndTime = new TimeOnly(22, 0),
            TimeZoneId = "America/New_York",
            Location = "Metro County Courthouse (after hours)",
            OrganizationId = DemoOrgId,
            CreatedBy = Guid.Empty, // TODO: Use actual user ID once ApplicationUsers are seeded
            ModifiedBy = Guid.Empty
        };
    }

    private static Exercise CreateFloodTrainingExercise(DateTime now)
    {
        return new Exercise
        {
            Id = FloodTrainingId,
            Name = "Flash Flood Response Training",
            Description = "Practice exercise for new EOC staff to familiarize themselves with flood response " +
                          "procedures and the Cadence MSEL management system.",
            ExerciseType = ExerciseType.TTX,
            Status = ExerciseStatus.Draft,
            IsPracticeMode = true, // Practice mode - excluded from reports
            ScheduledDate = DateOnly.FromDateTime(now.AddDays(7)),
            StartTime = new TimeOnly(13, 0),
            EndTime = new TimeOnly(15, 0),
            TimeZoneId = "America/New_York",
            Location = "Metro County EOC, Training Room",
            OrganizationId = DemoOrgId,
            CreatedBy = Guid.Empty, // TODO: Use actual user ID once ApplicationUsers are seeded
            ModifiedBy = Guid.Empty
        };
    }

    #endregion

    #region MSELs

    private static Msel CreateHurricaneMsel()
    {
        return new Msel
        {
            Id = HurricaneMselId,
            Name = "Hurricane TTX MSEL v1.0",
            Description = "Master Scenario Events List for Hurricane Response TTX. " +
                          "Scenario: Hurricane Maria, Category 3, making landfall on Metro County coast.",
            Version = 1,
            IsActive = true,
            ExerciseId = HurricaneTtxId,
            CreatedBy = Guid.Empty, // TODO: Use actual user ID once ApplicationUsers are seeded
            ModifiedBy = Guid.Empty
        };
    }

    private static Msel CreateFloodMsel()
    {
        return new Msel
        {
            Id = FloodMselId,
            Name = "Flood Training MSEL",
            Description = "Simplified MSEL for training purposes.",
            Version = 1,
            IsActive = true,
            ExerciseId = FloodTrainingId,
            CreatedBy = Guid.Empty, // TODO: Use actual user ID once ApplicationUsers are seeded
            ModifiedBy = Guid.Empty
        };
    }

    #endregion

    #region Phases

    private static List<Phase> CreateHurricanePhases()
    {
        return new List<Phase>
        {
            new Phase
            {
                Id = Phase1Id,
                Name = "Phase 1: Warning & Preparation",
                Description = "72-48 hours before landfall. Focus on warning dissemination, EOC activation, " +
                              "and protective action decisions.",
                Sequence = 1,
                ExerciseId = HurricaneTtxId,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },
            new Phase
            {
                Id = Phase2Id,
                Name = "Phase 2: Evacuation & Shelter",
                Description = "48-12 hours before landfall. Mandatory evacuation execution, shelter activation, " +
                              "and special needs population support.",
                Sequence = 2,
                ExerciseId = HurricaneTtxId,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },
            new Phase
            {
                Id = Phase3Id,
                Name = "Phase 3: Response & Life Safety",
                Description = "Landfall through 24 hours post. Emergency response operations, search and rescue, " +
                              "and critical infrastructure protection.",
                Sequence = 3,
                ExerciseId = HurricaneTtxId,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            }
        };
    }

    #endregion

    #region Objectives

    private static List<Objective> CreateHurricaneObjectives()
    {
        return new List<Objective>
        {
            new Objective
            {
                Id = Objective1Id,
                ObjectiveNumber = "1",
                Name = "EOC Activation & Coordination",
                Description = "Demonstrate the ability to activate the Emergency Operations Center within 2 hours " +
                              "of notification and establish effective coordination with all ESF partners.",
                ExerciseId = HurricaneTtxId,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },
            new Objective
            {
                Id = Objective2Id,
                ObjectiveNumber = "2",
                Name = "Public Warning & Information",
                Description = "Demonstrate the ability to disseminate timely and accurate public warnings through " +
                              "multiple channels including EAS, social media, and direct notification systems.",
                ExerciseId = HurricaneTtxId,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },
            new Objective
            {
                Id = Objective3Id,
                ObjectiveNumber = "3",
                Name = "Evacuation Operations",
                Description = "Demonstrate the ability to execute mandatory evacuation orders including traffic " +
                              "management, transportation support, and special needs population assistance.",
                ExerciseId = HurricaneTtxId,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },
            new Objective
            {
                Id = Objective4Id,
                ObjectiveNumber = "4",
                Name = "Mass Care & Shelter Operations",
                Description = "Demonstrate the ability to activate and manage emergency shelters with adequate " +
                              "capacity, supplies, and staffing for displaced residents.",
                ExerciseId = HurricaneTtxId,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            }
        };
    }

    #endregion

    #region Hurricane Injects

    private static List<Inject> CreateHurricaneInjects(DateTime now)
    {
        var firedTime1 = now.AddHours(-2);
        var firedTime2 = now.AddHours(-1).AddMinutes(-45);
        var firedTime3 = now.AddHours(-1).AddMinutes(-30);

        return new List<Inject>
        {
            // ===== PHASE 1: Warning & Preparation =====

            // Inject 1 - FIRED (completed)
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 1,
                Title = "NWS Issues Hurricane Watch",
                Description = "The National Weather Service has issued a Hurricane Watch for Metro County and " +
                              "surrounding coastal areas. Hurricane Maria is currently a Category 2 storm located " +
                              "450 miles southeast, moving northwest at 12 mph. Conditions are expected to " +
                              "deteriorate within 48 hours.",
                ScheduledTime = new TimeOnly(9, 0),
                ScenarioDay = 1,
                ScenarioTime = new TimeOnly(8, 0),
                Target = "EOC Director",
                Source = "National Weather Service",
                DeliveryMethod = DeliveryMethod.Email,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 1,
                ExpectedAction = "Activate EOC to Level 2 (Partial Activation). Notify department heads and key stakeholders. " +
                                 "Begin monitoring NWS updates.",
                ControllerNotes = "Provide printed NWS briefing package. This inject starts the exercise.",
                FiredAt = firedTime1,
                FiredBy = null,
                MselId = HurricaneMselId,
                PhaseId = Phase1Id,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },

            // Inject 2 - FIRED (completed)
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 2,
                Title = "Media Inquiry - Storm Preparations",
                Description = "WMET-TV News is requesting an interview with the Emergency Manager regarding " +
                              "county storm preparations. Reporter asks: 'What should residents be doing right now?'",
                ScheduledTime = new TimeOnly(9, 15),
                ScenarioDay = 1,
                ScenarioTime = new TimeOnly(10, 30),
                Target = "Public Information Officer",
                Source = "WMET-TV News",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 2,
                ExpectedAction = "Coordinate response with EOC Director. Prepare talking points on preparedness actions. " +
                                 "Schedule interview or provide written statement.",
                ControllerNotes = "Evaluate PIO's messaging consistency with EOC operations. Note coordination with EOC Director.",
                FiredAt = firedTime2,
                FiredBy = null,
                MselId = HurricaneMselId,
                PhaseId = Phase1Id,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },

            // Inject 3 - FIRED (completed)
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 3,
                Title = "School District Inquiry",
                Description = "Metro County School District Superintendent calls EOC asking for guidance on " +
                              "school closures. 'We have 45,000 students and need to make a decision by 3 PM today " +
                              "for tomorrow's schedule.'",
                ScheduledTime = new TimeOnly(9, 30),
                ScenarioDay = 1,
                ScenarioTime = new TimeOnly(13, 0),
                Target = "EOC Director",
                Source = "School District",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Fired,
                Sequence = 3,
                ExpectedAction = "Provide current threat assessment. Coordinate with Schools ESF. Recommend decision " +
                                 "timeline based on forecast confidence.",
                ControllerNotes = "Tests coordination between EOC and school district. Note decision-making process.",
                FiredAt = firedTime3,
                FiredBy = null,
                MselId = HurricaneMselId,
                PhaseId = Phase1Id,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },

            // ===== PHASE 2: Evacuation & Shelter =====

            // Inject 4 - PENDING (next up)
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 4,
                Title = "Hurricane Warning Upgrade",
                Description = "NWS upgrades to Hurricane Warning. Maria now Category 3 with 120 mph sustained winds. " +
                              "Landfall expected in 36 hours. Storm surge forecast: 8-12 feet for Zone A, " +
                              "4-6 feet for Zone B.",
                ScheduledTime = new TimeOnly(9, 45),
                ScenarioDay = 2,
                ScenarioTime = new TimeOnly(6, 0),
                Target = "EOC Director",
                Source = "National Weather Service",
                DeliveryMethod = DeliveryMethod.Email,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 4,
                ExpectedAction = "Activate EOC to Level 1 (Full Activation). Issue mandatory evacuation order for Zone A. " +
                                 "Issue voluntary evacuation recommendation for Zone B.",
                ControllerNotes = "KEY DECISION POINT - Observe protective action decision-making. Provide updated NWS briefing.",
                MselId = HurricaneMselId,
                PhaseId = Phase2Id,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },

            // Inject 5 - PENDING
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 5,
                Title = "Special Needs Registry Transportation",
                Description = "Social Services reports 47 residents on the special needs registry in Zone A require " +
                              "transportation assistance for evacuation. 12 are wheelchair-bound, 8 require oxygen, " +
                              "and 3 are dialysis patients with scheduled treatments.",
                ScheduledTime = new TimeOnly(10, 0),
                ScenarioDay = 2,
                ScenarioTime = new TimeOnly(8, 0),
                Target = "Transportation Coordinator",
                Source = "Department of Social Services",
                DeliveryMethod = DeliveryMethod.Radio,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 5,
                ExpectedAction = "Coordinate paratransit vehicles and ambulances. Prioritize dialysis patients. " +
                                 "Establish pickup schedule and communicate to residents.",
                ControllerNotes = "Tests special needs evacuation coordination. Provide registry printout as prop.",
                MselId = HurricaneMselId,
                PhaseId = Phase2Id,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },

            // Inject 6 - PENDING (Contingency)
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 6,
                Title = "Evacuation Route Flooding (Contingency)",
                Description = "Highway Department reports flooding on Route 17 evacuation route due to high tide and " +
                              "rain. Road is impassable at Mile Marker 23.",
                ScheduledTime = new TimeOnly(10, 15),
                ScenarioDay = 2,
                ScenarioTime = new TimeOnly(10, 0),
                Target = "Transportation Coordinator",
                Source = "Highway Department",
                DeliveryMethod = DeliveryMethod.Radio,
                InjectType = InjectType.Contingency,
                Status = InjectStatus.Pending,
                Sequence = 6,
                ExpectedAction = "Activate alternate evacuation routes. Coordinate with law enforcement for traffic control. " +
                                 "Update public messaging on route changes.",
                ControllerNotes = "CONTINGENCY - Use if players complete Inject 5 quickly or need additional challenge. " +
                                  "Tests adaptability and alternate route planning.",
                FireCondition = "Use if players are ahead of schedule or evacuation discussion is too smooth",
                MselId = HurricaneMselId,
                PhaseId = Phase2Id,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },

            // Inject 7 - PENDING
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 7,
                Title = "Primary Shelter at Capacity",
                Description = "American Red Cross reports Northside High School shelter has reached 85% capacity " +
                              "(425 of 500 beds occupied) with evacuees still arriving. Current rate suggests " +
                              "capacity will be exceeded within 2 hours.",
                ScheduledTime = new TimeOnly(10, 30),
                ScenarioDay = 2,
                ScenarioTime = new TimeOnly(14, 0),
                Target = "Mass Care Coordinator",
                Source = "American Red Cross",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 7,
                ExpectedAction = "Activate secondary shelter at Westside Middle School. Coordinate transportation for " +
                                 "overflow. Update shelter status in WebEOC.",
                ControllerNotes = "Tests shelter management and resource allocation decisions.",
                MselId = HurricaneMselId,
                PhaseId = Phase2Id,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },

            // Inject 8 - PENDING
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 8,
                Title = "Nursing Home Evacuation Request",
                Description = "Sunny Acres Nursing Home in Zone B (voluntary evacuation area) requests county " +
                              "assistance with evacuation of 78 residents. Administrator states they cannot " +
                              "secure enough ambulances through normal channels.",
                ScheduledTime = new TimeOnly(10, 45),
                ScenarioDay = 2,
                ScenarioTime = new TimeOnly(16, 0),
                Target = "Medical Branch Coordinator",
                Source = "Sunny Acres Nursing Home",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 8,
                ExpectedAction = "Assess nursing home capabilities and patient acuity. Coordinate medical transport resources. " +
                                 "Identify receiving facilities with appropriate care levels.",
                ControllerNotes = "Tests medical surge coordination. Note prioritization decisions.",
                MselId = HurricaneMselId,
                PhaseId = Phase2Id,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },

            // ===== PHASE 3: Response & Life Safety =====

            // Inject 9 - PENDING
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 9,
                Title = "Storm Surge Flooding - Water Rescues",
                Description = "Metro Fire Department reports significant storm surge flooding in Zone A coastal areas. " +
                              "Multiple 911 calls for water rescue. At least 6 addresses with confirmed occupants " +
                              "trapped by rising water.",
                ScheduledTime = new TimeOnly(11, 0),
                ScenarioDay = 3,
                ScenarioTime = new TimeOnly(3, 0),
                Target = "Fire/Rescue Branch",
                Source = "Metro Fire Department",
                DeliveryMethod = DeliveryMethod.Radio,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 9,
                ExpectedAction = "Deploy swift water rescue teams. Prioritize calls by life threat. Request Coast Guard " +
                                 "assistance if needed. Establish rescue coordination with Law Enforcement.",
                ControllerNotes = "CRITICAL LIFE SAFETY - Observe prioritization and resource deployment decisions.",
                MselId = HurricaneMselId,
                PhaseId = Phase3Id,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },

            // Inject 10 - PENDING (Adaptive)
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 10,
                Title = "Hospital Generator Failure (Adaptive)",
                Description = "Metro General Hospital reports main generator failure. Running on backup power with " +
                              "approximately 4 hours of fuel. 23 patients in ICU, 8 on ventilators.",
                ScheduledTime = new TimeOnly(11, 15),
                ScenarioDay = 3,
                ScenarioTime = new TimeOnly(5, 0),
                Target = "Medical Branch Coordinator",
                Source = "Metro General Hospital",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Adaptive,
                Status = InjectStatus.Pending,
                Sequence = 10,
                ExpectedAction = "Coordinate emergency fuel delivery. Identify backup generator resources. " +
                                 "Prepare patient evacuation plan if power cannot be restored.",
                ControllerNotes = "ADAPTIVE - Fire this inject if players have good momentum. Creates cascading " +
                                  "decisions about critical infrastructure and medical surge.",
                FireCondition = "Fire if players successfully manage water rescues; skip if overwhelmed",
                MselId = HurricaneMselId,
                PhaseId = Phase3Id,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },

            // Inject 11 - PENDING
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 11,
                Title = "Widespread Power Outage",
                Description = "Metro Power reports 67,000 customers without power (approximately 45% of county). " +
                              "Multiple transmission lines damaged. Estimated restoration: 5-7 days for hardest hit areas.",
                ScheduledTime = new TimeOnly(11, 30),
                ScenarioDay = 3,
                ScenarioTime = new TimeOnly(8, 0),
                Target = "Infrastructure Branch",
                Source = "Metro Power Company",
                DeliveryMethod = DeliveryMethod.Email,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 11,
                ExpectedAction = "Coordinate generator allocation for critical facilities. Establish cooling/warming centers. " +
                                 "Update public messaging on restoration timeline.",
                ControllerNotes = "Tests long-term recovery planning during response phase.",
                MselId = HurricaneMselId,
                PhaseId = Phase3Id,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },

            // Inject 12 - SKIPPED (example of skipped inject)
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 12,
                Title = "Chemical Spill at Industrial Park",
                Description = "Reports of chemical release at Metro Industrial Park. Unknown substance. " +
                              "Facility is in flood zone with potential for waterway contamination.",
                ScheduledTime = new TimeOnly(11, 35),
                ScenarioDay = 3,
                ScenarioTime = new TimeOnly(9, 0),
                Target = "HazMat Coordinator",
                Source = "Metro Fire Department",
                DeliveryMethod = DeliveryMethod.Radio,
                InjectType = InjectType.Complexity,
                Status = InjectStatus.Skipped,
                Sequence = 12,
                ExpectedAction = "Dispatch HazMat team. Establish isolation perimeter. Coordinate with EPA and State DEQ.",
                ControllerNotes = "COMPLEXITY - Use only if players are handling scenario easily and need more challenge.",
                FireCondition = "Exercise Director discretion based on player performance",
                SkippedAt = now.AddMinutes(-30),
                SkippedBy = null,
                SkipReason = "Time constraints - exercise running behind schedule. Saved for future exercise.",
                MselId = HurricaneMselId,
                PhaseId = Phase3Id,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },

            // Inject 13 - PENDING
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 13,
                Title = "Governor's Press Conference",
                Description = "Governor's office announces press conference in 30 minutes. Requests county update on " +
                              "damage assessment, shelter population, and resource needs for state/federal assistance request.",
                ScheduledTime = new TimeOnly(11, 45),
                ScenarioDay = 3,
                ScenarioTime = new TimeOnly(10, 0),
                Target = "EOC Director",
                Source = "Governor's Office",
                DeliveryMethod = DeliveryMethod.Phone,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 13,
                ExpectedAction = "Compile situation report with current statistics. Prepare resource request documentation. " +
                                 "Coordinate talking points with PIO.",
                ControllerNotes = "Tests information management and external coordination. Good capstone inject.",
                MselId = HurricaneMselId,
                PhaseId = Phase3Id,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },

            // Inject 14 - PENDING (EndEx)
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 14,
                Title = "ENDEX - Exercise Termination",
                Description = "The Exercise Director announces termination of the exercise. All play stops. " +
                              "Hot wash will begin in 15 minutes in the main EOC.",
                ScheduledTime = new TimeOnly(11, 55),
                ScenarioDay = 3,
                ScenarioTime = new TimeOnly(12, 0),
                Target = "All Players",
                Source = "Exercise Director",
                DeliveryMethod = DeliveryMethod.Verbal,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 14,
                ExpectedAction = "Cease all exercise play. Transition to hot wash discussion. " +
                                 "Complete player feedback forms.",
                ControllerNotes = "Read ENDEX statement. Ensure all players heard. Direct to hot wash location.",
                MselId = HurricaneMselId,
                PhaseId = Phase3Id,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            }
        };
    }

    #endregion

    #region Flood Training Injects (Practice Mode)

    private static List<Inject> CreateFloodInjects()
    {
        return new List<Inject>
        {
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 1,
                Title = "Flash Flood Watch Issued",
                Description = "NWS issues Flash Flood Watch for Metro County. Heavy rainfall expected over next 6 hours.",
                ScheduledTime = new TimeOnly(13, 0),
                ScenarioDay = 1,
                ScenarioTime = new TimeOnly(6, 0),
                Target = "EOC Director",
                Source = "National Weather Service",
                DeliveryMethod = DeliveryMethod.Email,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 1,
                ExpectedAction = "Monitor weather updates. Alert field crews.",
                ControllerNotes = "Training inject - walk through EOC activation process.",
                MselId = FloodMselId,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 2,
                Title = "Road Flooding Reports",
                Description = "Highway department reports multiple roads with water over roadway in low-lying areas.",
                ScheduledTime = new TimeOnly(13, 30),
                ScenarioDay = 1,
                ScenarioTime = new TimeOnly(10, 0),
                Target = "Transportation Coordinator",
                Source = "Highway Department",
                DeliveryMethod = DeliveryMethod.Radio,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 2,
                ExpectedAction = "Coordinate road closures. Update WebEOC road status.",
                ControllerNotes = "Training inject - demonstrate road closure coordination process.",
                MselId = FloodMselId,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },
            new Inject
            {
                Id = Guid.NewGuid(),
                InjectNumber = 3,
                Title = "End Training Exercise",
                Description = "Training exercise complete. Debrief to follow.",
                ScheduledTime = new TimeOnly(14, 45),
                ScenarioDay = 1,
                ScenarioTime = new TimeOnly(12, 0),
                Target = "All Participants",
                Source = "Exercise Controller",
                DeliveryMethod = DeliveryMethod.Verbal,
                InjectType = InjectType.Standard,
                Status = InjectStatus.Pending,
                Sequence = 3,
                ExpectedAction = "Complete training feedback form.",
                ControllerNotes = "End training session. Collect feedback.",
                MselId = FloodMselId,
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            }
        };
    }

    #endregion

    #region Participants

    private static List<ExerciseParticipant> CreateHurricaneParticipants(DateTime now)
    {
        return new List<ExerciseParticipant>
        {
            new ExerciseParticipant
            {
                Id = Guid.NewGuid(),
                ExerciseId = HurricaneTtxId,
                UserId = AdminUserId,
                Role = ExerciseRole.Administrator,
                AssignedAt = now.AddDays(-7),
                AssignedById = AdminUserId,
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-7),
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },
            new ExerciseParticipant
            {
                Id = Guid.NewGuid(),
                ExerciseId = HurricaneTtxId,
                UserId = DirectorUserId,
                Role = ExerciseRole.ExerciseDirector,
                AssignedAt = now.AddDays(-7),
                AssignedById = AdminUserId,
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-7),
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },
            new ExerciseParticipant
            {
                Id = Guid.NewGuid(),
                ExerciseId = HurricaneTtxId,
                UserId = Controller1UserId,
                Role = ExerciseRole.Controller,
                AssignedAt = now.AddDays(-5),
                AssignedById = DirectorUserId,
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-5),
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },
            new ExerciseParticipant
            {
                Id = Guid.NewGuid(),
                ExerciseId = HurricaneTtxId,
                UserId = Controller2UserId,
                Role = ExerciseRole.Controller,
                AssignedAt = now.AddDays(-5),
                AssignedById = DirectorUserId,
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-5),
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },
            new ExerciseParticipant
            {
                Id = Guid.NewGuid(),
                ExerciseId = HurricaneTtxId,
                UserId = EvaluatorUserId,
                Role = ExerciseRole.Evaluator,
                AssignedAt = now.AddDays(-3),
                AssignedById = DirectorUserId,
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now.AddDays(-3),
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            },
            new ExerciseParticipant
            {
                Id = Guid.NewGuid(),
                ExerciseId = HurricaneTtxId,
                UserId = ObserverUserId,
                Role = ExerciseRole.Observer,
                AssignedAt = now.AddDays(-1),
                AssignedById = DirectorUserId,
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1),
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            }
        };
    }

    private static List<ExerciseParticipant> CreateFloodParticipants(DateTime now)
    {
        return new List<ExerciseParticipant>
        {
            new ExerciseParticipant
            {
                Id = Guid.NewGuid(),
                ExerciseId = FloodTrainingId,
                UserId = Controller1UserId,
                Role = ExerciseRole.ExerciseDirector,
                AssignedAt = now.AddDays(-2),
                AssignedById = Controller1UserId,
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-2),
                CreatedBy = Guid.Empty,
                ModifiedBy = Guid.Empty
            }
        };
    }

    #endregion
}
