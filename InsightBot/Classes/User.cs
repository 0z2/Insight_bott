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
        public List<Insight> Insights { get; set; } = new();
        public int NumberOfLastThought { get; set; }
        public bool WantToAddAnInsight { get; set; }
        
        public User(long telegramId)
        {
            
            TelegramId = telegramId;
            WantToAddAnInsight = false;

            NumberOfLastThought = 0;
        }

        public void GetCurrentThought(out string textOfCurrentInsight, out int idInsightInDb)
        {
            {
                Insight currentInsight = Insights[NumberOfLastThought];

                if (Insights.Count-1 == NumberOfLastThought)
                {
                    NumberOfLastThought = 0;
                }
                else 
                {
                    NumberOfLastThought++;
                }
                
                textOfCurrentInsight = currentInsight.TextOfInsight;
                idInsightInDb = currentInsight.Id;
            }
            
        }

        public void AddNewInsight(string textOfInsight)
        {
            var newInsight = new Insight(textOfInsight);
            Insights.Add(newInsight);
        }
    }
}
