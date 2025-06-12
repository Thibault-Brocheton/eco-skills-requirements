namespace SkillsRequirements
{
    using Eco.Core.Plugins.Interfaces;
    using Eco.Core.Plugins;
    using Eco.Core.Utils;
    using Eco.Gameplay.Players;
    using Eco.Gameplay.Skills;
    using Eco.Gameplay.Systems.Messaging.Chat.Commands;
    using Eco.Shared.Logging;
    using Eco.Shared.Utils;
    using Eco.Simulation.Time;
    using System.Collections.Generic;
    using System.Linq;
    using System;

    public class SkillsRequirementsMod: IModInit
    {
        public static ModRegistration Register() => new()
        {
            ModName = "SkillsRequirements",
            ModDescription = "SkillsRequirements allows server admins to define user requirements for skill learn.",
            ModDisplayName = "Skills Requirements"
        };
    }

    public class SkillsRequirementsConfig : Singleton<SkillsRequirementsConfig>
    {
        public bool Enabled { get; set; } = true;

        public Dictionary<string, List<string>> ForbiddenSkills { get; set; } = new()
        {
            { "SomeSkillIWantToLearn", new List<string>{"SomeSkillIShouldNotHave", "AnotherOneIShouldNotHave"} },
        };

        public Dictionary<string, List<string>> MandatorySkills { get; set; } = new()
        {
            { "SomeSkillIWantToLearn", new List<string>{"SomeSkillIShouldHave", "AnotherOneIShouldHave"} },
        };

        public Dictionary<string, float> DontLearnBeforeDay { get; set; } = new()
        {
            { "SomeSkillIWantToLearn", 3.5f},
        };

        public Dictionary<string, int> CustomStarCost { get; set; } = new()
        {
            { "SomeSkillIWantToLearn", 3},
        };

        public Dictionary<string, List<string>> BypassUsers { get; set; } = new();
    }

    [Priority(PriorityAttribute.VeryLow)]
    public class SkillsRequirementsPlugin : Singleton<SkillsRequirementsPlugin>, IModKitPlugin, IInitializablePlugin, IConfigurablePlugin
    {
        public IPluginConfig PluginConfig => this.config;
        private readonly PluginConfig<SkillsRequirementsConfig> config;
        public SkillsRequirementsConfig Config => this.config.Config;
        public ThreadSafeAction<object, string> ParamChanged { get; set; } = new();

        private bool isActivated;

        public SkillsRequirementsPlugin()
        {
            this.config = new PluginConfig<SkillsRequirementsConfig>("SkillsRequirements");
            this.SaveConfig();
        }

        public string GetStatus()
        {
            return "OK";
        }

        public string GetCategory()
        {
            return "Mods";
        }

        public void Initialize(TimedTask timer)
        {
            Log.WriteLineLoc($"[SkillsRequirements] Activate World OnBlockChanged");

            if (Obj.Config.Enabled)
            {
                this.Activate();
            }
        }

        private static readonly Func<User, Type, int> DummyCalculateStarsNeededForSpecialty = (_,_) => 1;

        private static readonly Func<User, Type, int> CalculateStarsNeededForSpecialty = (User user, Type skillType) =>
        {
            var currentSkillName = skillType.Name;
            bool canLearn = true;

            if (Obj.Config.BypassUsers.TryGetValue(user.StrangeId, out var bypassUser) && bypassUser.Contains(currentSkillName))
            {
                user.InfoBoxLoc($"[SkillsRequirements Mod] You can bypass requirements for skill {currentSkillName}!");
            }
            else
            {
                if (Obj.Config.ForbiddenSkills.TryGetValue(currentSkillName, out var forbiddenSkills))
                {
                    foreach (var forbiddenSkill in forbiddenSkills)
                    {
                        var foundSkill = user.Skillset.Skills.FirstOrDefault(s => s.Name == forbiddenSkill);

                        if (foundSkill is not { Level: >= 1 }) continue;

                        user.ErrorLocStr($"[SkillsRequirements Mod] You can't learn skill {currentSkillName} because you have the skill {forbiddenSkill}!");
                        canLearn = false;
                    }
                }

                if (Obj.Config.MandatorySkills.TryGetValue(currentSkillName, out var mandatorySkills))
                {
                    foreach (var mandatorySkill in mandatorySkills)
                    {
                        var foundSkill = user.Skillset.Skills.FirstOrDefault(s => s.Name == mandatorySkill);

                        if (foundSkill is { Level: >= 1 }) continue;

                        user.ErrorLocStr($"[SkillsRequirements Mod] You can't learn skill {currentSkillName} because you don't have the skill {mandatorySkill}!");
                        canLearn = false;
                    }
                }

                if (Obj.Config.DontLearnBeforeDay.TryGetValue(currentSkillName, out var dayBefore))
                {
                    // Retrieve the required day for the skill type TSkill from the Planning dictionary.
                    if (WorldTime.Day < dayBefore)
                    {
                        user.ErrorLocStr($"[SkillsRequirements Mod] You can't learn skill {currentSkillName} before day {dayBefore}. It is currently day {Text.Num(WorldTime.Day)}.");
                        canLearn = false;
                    }
                }
            }

            if (canLearn)
            {
                var cost = Obj.Config.CustomStarCost.GetValueOrDefault(currentSkillName, 1);

                if (cost > 1)
                {
                    user.InfoBoxLoc($"[SkillsRequirements Mod] The skill {currentSkillName} requires {cost} stars.");

                    return cost;
                }
            }

            return int.MaxValue;
        };

        private void Activate()
        {
            if (this.isActivated) return;
            Skill.CalculateStarsNeededForSpecialty = CalculateStarsNeededForSpecialty;
            this.isActivated = true;
        }

        private void DeActivate()
        {
            if (!this.isActivated) return;
            Skill.CalculateStarsNeededForSpecialty = DummyCalculateStarsNeededForSpecialty;
            this.isActivated = false;
        }

        public bool Toggle()
        {
            if (this.isActivated)
            {
                this.DeActivate();
            }
            else
            {
                this.Activate();
            }

            return this.isActivated;
        }

        public object GetEditObject() => this.config.Config;

        public void OnEditObjectChanged(object o, string param)
        {
            this.SaveConfig();
        }
    }

    [ChatCommandHandler]
    public static class SkillsRequirementsChatCommand
    {
        [ChatCommand("SkillsRequirements - Admin Commands")]
        public static void SkillsRequirements() { }

        [ChatSubCommand("SkillsRequirements", "Toggle skills requirements", ChatAuthorizationLevel.Admin)]
        public static void Toggle(User user, bool save = false)
        {
            var isActivated = SkillsRequirementsPlugin.Obj.Toggle();

            if (save)
            {
                SkillsRequirementsPlugin.Obj.Config.Enabled = isActivated;
                SkillsRequirementsPlugin.Obj.SaveConfig();
            }

            user.MsgLocStr($"SkillsRequirements are now {(isActivated ? "enabled" : "disabled")}.");
        }

        [ChatSubCommand("SkillsRequirements", "Allow a user to bypass requirements (will still need the custom amount of stars if set)", ChatAuthorizationLevel.Admin)]
        public static void AllowBypass(User user, User targetUser, string skillName)
        {
            if (Skill.AllSkills.FirstOrDefault(s => s.Name == skillName.Trim()) == null)
            {
                user.ErrorLocStr($"Can't find skill {skillName}");
                return;
            }

            SkillsRequirementsPlugin.Obj.Config.BypassUsers.AddOrUpdate(targetUser.StrangeId, new List<string> { skillName }, (oldList, newList) =>
            {
                if (!oldList.Contains(newList.First()))
                {
                    oldList.AddRange(newList);
                }

                return oldList;
            });
            SkillsRequirementsPlugin.Obj.SaveConfig();

            user.MsgLocStr($"{targetUser.Name} will be allowed to learn {skillName} without requirements.");
        }

        [ChatSubCommand("SkillsRequirements", "Disallow a user to bypass requirements", ChatAuthorizationLevel.Admin)]
        public static void DisallowBypass(User user, User targetUser, string skillName)
        {
            if (Skill.AllSkills.FirstOrDefault(s => s.Name == skillName) == null)
            {
                user.ErrorLocStr($"Can't find skill {skillName}");
                return;
            }

            if (!SkillsRequirementsPlugin.Obj.Config.BypassUsers.TryGetValue(targetUser.StrangeId, out var userConf)) return;
            userConf.Remove(skillName);
            SkillsRequirementsPlugin.Obj.SaveConfig();

            user.MsgLocStr($"{targetUser} will not be allowed anymore to learn {skillName} without requirements.");
        }

        [ChatSubCommand("SkillsRequirements", "Update DontLearnBeforeDay value of a skill", ChatAuthorizationLevel.Admin)]
        public static void UpdateDontLearnBeforeDay(User user, string skillName, float day)
        {
            if (Skill.AllSkills.FirstOrDefault(s => s.Name == skillName) == null)
            {
                user.ErrorLocStr($"Can't find skill {skillName}");
                return;
            }

            if (day < 0f)
            {
                user.ErrorLocStr($"Day can't be negative");
                return;
            }

            SkillsRequirementsPlugin.Obj.Config.DontLearnBeforeDay[skillName] = day;
            SkillsRequirementsPlugin.Obj.SaveConfig();

            user.MsgLocStr($"{skillName} can now be learned only until day {day} is reached. Current day is {Text.Num(WorldTime.Day)}");
        }
    }
}
