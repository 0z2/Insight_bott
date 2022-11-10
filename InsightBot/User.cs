using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insight_bott
{
    public class User
    {
        [Key]
        public long TelegramId { get; set; }
        public List<Insight> Insights; 
        public int NumberOfLastThought { get; set; }
        
        public bool WantToAddAnInsight { get; set; }
        
        
        public User(long telegramId)
        {
            
            TelegramId = telegramId;
            //WantToAddAnInsight = false;
            
            var startInsights = new List<string>()
            {
                "Глаза боятся - руки делают!", "Тише едешь - дальше будешь!", "Утро вечера мудренее!"
            };
            
            Insights = new List<Insight>();
            foreach (var textOfInsight in startInsights)
            {
                var newInsight = new Insight(textOfInsight, telegramId);
                Insights.Add(newInsight);
            }
            NumberOfLastThought = 0;
        }

        public string GetCurrentThought()
        {
            //using (ApplicationContext db = new ApplicationContext()) //подключаемся к контексту БД
            {
                Insight currentInsight = Insights[NumberOfLastThought];

                // если мысль является последней, то начинаем сначала
                if (Insights.Count-1 == NumberOfLastThought)
                {
                    NumberOfLastThought = 0;
                }
                else 
                {
                    NumberOfLastThought++;
                }
                
               // db.SaveChangesAsync(); // разобраться что это за токен такой и для чего он нужен

                return $"{currentInsight.TextOfInsight} - id {currentInsight.InsightId}";
                
            }
            
        }

        public void AddNewInsight(string textOfInsight)
        {
            var newInsight = new Insight(textOfInsight, TelegramId);
            Insights.Add(newInsight);
        }
    }
}
