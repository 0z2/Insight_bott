using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Insight_bott;

public class Insight
{

    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int InsightId { get; set; }
    
    // разобраться почему связь не образауется и данные не попадают в базу!
    public long UserTelegramId { get; set; }      // внешний ключ

    public string TextOfInsight { get; set; }
    
    public Insight(string textOfInsight, long userTelegramId)
    {
        TextOfInsight = textOfInsight;
        UserTelegramId = userTelegramId;
    }
}