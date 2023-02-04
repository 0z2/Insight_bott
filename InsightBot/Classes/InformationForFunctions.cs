namespace Insight_bott;

public class IInformationForFunctions
{
    public string TextOfMessage = "";
}

public class InformationForSingleReptition : IInformationForFunctions
{
    public int InHowManyDaysToRepeat;

}
public class InformationForRegularReptition : IInformationForFunctions
{
    public int HowOftenInDaysRepeat;

}
public class InformationForClearReptition : IInformationForFunctions
{

}