namespace Insight_bott;

public class Insight
{
    public int Id { get; set; }
    public string TextOfInsight { get; set; }
    
    public long UserTelegramId { get; set;  }      // внешний ключ
    public User? User { get; set; }      // навигационное свойство

    
    public int? HowOftenRepeatInDays { get; set; }
    public DateTime? WhenToRepeat { get; set; }
    public DateTime? DayOfLastRepeat { get; set; }
    
    public Insight(string textOfInsight)
    {
        TextOfInsight = textOfInsight;
    }

    public void CreateSingleRepeatition(int afterHowManyDaysRepeat)
    {
        var afterHowManyDaysRepeatSpan = new System.TimeSpan(afterHowManyDaysRepeat, 0, 0, 0);
        WhenToRepeat = DateTime.Today + afterHowManyDaysRepeatSpan;
    }
    public void CreateRegularRepeatition(int howOftenRepeatInDays)
    {
        HowOftenRepeatInDays = howOftenRepeatInDays;
        DayOfLastRepeat = DateTime.Today;
    }
}