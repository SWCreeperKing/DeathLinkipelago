namespace DeathLinkipelago.Scripts;

public class Config(long secondsPerLifeCoin, long deathCheckAmount, bool sendTrapsAfterGoal, bool hasFunnyButton, bool useGlobalCounter, bool sendScoutHints)
{
    public long SecondsPerLifeCoin = secondsPerLifeCoin;
    public long DeathCheckAmount = deathCheckAmount;
    public bool SendTrapsAfterGoal = sendTrapsAfterGoal;
    public bool HasFunnyButton = hasFunnyButton;
    public bool UseGlobalCounter = useGlobalCounter;
    public bool SendScoutHints = sendScoutHints;
}