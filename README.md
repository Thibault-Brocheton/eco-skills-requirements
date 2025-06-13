# Eco Skills Requirements

This mod allows you to configure requirements for skills, so when a user wants to learn it by spending a star, he will have to:
- already have some other skills
- not have some skills
- wait before a specific day
- spend more or less than 1 star

As an admin you can run a command to allow some specific user to bypass these requirements.

## Commands

* /SkillsRequirements
  * /SkillsRequirements Toggle true?                              : Disable or Enable the mod. Add true if you want to save this change in the config file.
  * /SkillsRequirements AllowBypass User, skillName               : Allow a player to bypass requirements of a skill (He will still need the custom amont of stars, if set)
  * /SkillsRequirements DisallowBypass User, skillName            : Disallow a player to bypass requirements of a skill
  * /SkillsRequirements UpdateDontLearnBeforeDay skillName, day   : Update the day before a skill can be learned, without having to restart the server

## Configuration

The mod is configured with a SkillsRequirements.eco file in Configs (generated at the first start)
With following structure:

```
{
  "Enabled": true,
  "ForbiddenSkills": {
    "GatheringSkill": [
      "FarmingSkill",
      "CampfireCookingSkill"
    ],
    "FarmingSkill": [
      "GatheringSkill"
    ],
    "FertilizersSkill": [
      "ButcherySkill"
    ],
  },
  "MandatorySkills": {
    "FertilizersSkill": [
      "GatheringSkill"
    ],
  },
  "DontLearnBeforeDay": {
    "GatheringSkill": 2.3,
    "HuntingSkill": 4.0
  },
  "CustomStarCost": {
    "HuntingSkill": 3
  },
  "BypassUsers": {}
}
```

## Installation

Copy the file SkillsRequirementsPlugin.cs in Mods/UserCode folder.
