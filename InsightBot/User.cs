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
        
        
        public User(long telegramId)
        {
            
            TelegramId = telegramId;
            
            var startInsights = new List<string>()
            {
                "Глаза боятся - руки делают!", "Тише едешь - дальше будешь!", "Утро вечера мудренее!"
            };
            
            Insights = new List<Insight>();
            foreach (var textOfInsight in startInsights)
            {
                
                var newTextOfInsight = new Insight(textOfInsight, telegramId);
                Insights.Add(newTextOfInsight);
            }
            NumberOfLastThought = 0;
        }

        public string GetCurrentThought()
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

            return $"{currentInsight.TextOfInsight} - id {currentInsight.InsightId}";
        }

    }
}
