using System.ComponentModel.DataAnnotations;


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

        public void GetCurrentInsight(out string textOfCurrentInsight, out int idInsightInDb)
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
                DbHelper.db.SaveChangesAsync();
            }
            
        }

        public void GetRandomInsight(out string textOfRandomInsight, out int idRandomInsight)
        {
            Random rnd = new Random();
            int amountOfInsights = Insights.Count();
            int randomNumber  = rnd.Next(0, amountOfInsights-1);
            textOfRandomInsight = Insights[randomNumber].TextOfInsight;
            idRandomInsight = Insights[randomNumber].Id;
        }

        public void AddNewInsight(string textOfInsight)
        {
            var newInsight = new Insight(textOfInsight);
            Insights.Add(newInsight);
        }
    }
}
