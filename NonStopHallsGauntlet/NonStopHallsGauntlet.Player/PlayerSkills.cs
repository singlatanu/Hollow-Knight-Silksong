namespace NonStopHallsGauntlet.Player

{
    internal static class PlayerSkills
    {
        public static void Skills()
        {
            PlayerData pd = PlayerData.instance;

            pd.hasDash = true;
            pd.hasBrolly = true;
            pd.hasWalljump = true;
            pd.hasDoubleJump = true;
            pd.hasHarpoonDash = true;
            pd.hasSilkBossNeedle = true;
            pd.hasThreadSphere = true;
            pd.hasNeedleThrow = true;
            pd.hasSilkCharge = true;
            pd.hasSilkBomb = true;
            pd.hasParry = true;

            pd.nailUpgrades = Plugin.nailUpgrade.Value;
            pd.silkRegenMax = Plugin.numSilkHeart.Value;
        }
    }
}