namespace Insight_bott;

public class Insight
{
    public int Id { get; set; }
    public string TextOfInsight { get; set; }
    
    public long UserTelegramId { get; set; }      // внешний ключ
    public User? User { get; set; }      // навигационное свойство
    
    public Insight(string textOfInsight)
    {
        TextOfInsight = textOfInsight;
    }
}