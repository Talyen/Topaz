using System;

namespace Topaz.LoopStudy
{
    /// <summary>Character-owned preparation and exertion rules; durations use World hours.</summary>
    public static class SurvivalRules
    {
        public const float BaseStamina = 100f;
        public const float RestedStamina = 125f;
        public const float BaseRegeneration = 20f;
        public const float RegenerationDelay = 1f;
        public const float RecoveryMultiplier = .9f;
        public const float JumpHeightMultiplier = 1.1f;
        public const double RestedHours = 24d;
        public const double BerryHours = 12d;
        public const double StewHours = 24d;
        public const double ForageRenewalHours = 24d;
        public const string BerriesId = "food.red-berries";
        public const string MushroomsId = "food.mushrooms";
        public const string StewId = "food.mushroom-stew";

        public static float Maximum(TopazCharacterData character) =>
            character != null && character.restedHours > 0d ? RestedStamina : BaseStamina;

        public static float Regeneration(TopazCharacterData character) =>
            BaseRegeneration * (character == null || character.foodHours <= 0d ? 1f :
                character.foodTier >= 2 ? 1.5f : 1.2f);

        public static bool CanEat(TopazCharacterData character, int tier) =>
            character != null && tier >= 1 && tier <= 2 &&
            (character.foodHours <= 0d || character.foodTier < tier);

        public static bool Eat(TopazCharacterData character, int tier)
        {
            if (!CanEat(character, tier)) return false;
            character.foodTier = tier;
            character.foodHours = tier == 2 ? StewHours : BerryHours;
            return true;
        }

        public static void Rest(TopazCharacterData character)
        {
            if (character == null) return;
            character.restedHours = RestedHours;
            character.stamina = RestedStamina;
        }

        public static void Advance(TopazCharacterData character, double worldHours)
        {
            if (character == null || worldHours <= 0d) return;
            character.restedHours = Math.Max(0d, character.restedHours - worldHours);
            character.foodHours = Math.Max(0d, character.foodHours - worldHours);
            if (character.foodHours == 0d) character.foodTier = 0;
            character.stamina = Math.Min(character.stamina, Maximum(character));
        }

        public static bool Spend(TopazCharacterData character, float cost)
        {
            if (character == null || cost <= 0f || character.stamina < cost) return false;
            character.stamina -= cost;
            return true;
        }

        public static void Refill(TopazCharacterData character, float seconds)
        {
            if (character == null || seconds <= 0f) return;
            character.stamina = Math.Min(Maximum(character),
                character.stamina + Regeneration(character) * seconds);
        }
    }
}
