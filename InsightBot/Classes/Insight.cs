namespace Insight_bott;

public class Insight
{
    public int Id { get; set; }
    public string TextOfInsight { get; set; }
    
    public long UserTelegramId { get; set;  }      // внешний ключ
    public User? User { get; set; }      // навигационное свойство

    public DateTime? WhenToRepeat { get; set; }
    
    public Insight(string textOfInsight)
    {
        TextOfInsight = textOfInsight;
    }

    public void CreateRepeat(int afterHowManyDaysRepeat)
    {
        var afterHowManyDaysRepeatSpan = new System.TimeSpan(afterHowManyDaysRepeat, 0, 0, 0);
        WhenToRepeat = DateTime.Today + afterHowManyDaysRepeatSpan;
    }
}