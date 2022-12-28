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
        
        public bool isAsign { get; set; }
        
        public User(long telegramId)
        {
            TelegramId = telegramId;
            WantToAddAnInsight = false;
            isAsign = true;

            NumberOfLastThought = 0;
        }

        public void GetCurrentInsight(out string textOfCurrentInsight, out int idInsightInDb)
        {
            {
                var numberOfLastInsightInList = Insights.Count - 1;
                if (NumberOfLastThought > numberOfLastInsightInList)
                {
                    NumberOfLastThought = 0;
                }

                Insight currentInsight = Insights[NumberOfLastThought];
                NumberOfLastThought++;
                
                textOfCurrentInsight = currentInsight.TextOfInsight;
                idInsightInDb = currentInsight.Id;
                DbHelper.db.SaveChangesAsync();
            }
        }

        public void GetInsightById(int idOfInsight, out string textOfCurrentInsight)
        {
            foreach (Insight insight in Insights)
            {
                if (insight.Id == idOfInsight)
                {
                    textOfCurrentInsight = insight.TextOfInsight;
                    break;
                }
            }
            textOfCurrentInsight = null;
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

        public void DeleteInsight(int insightIdNeedToDelete, out bool isDelited)
        {
            var numberOfDelitedInsightInList = 0;
            isDelited = false;
            // находим инсайт, который нужно удалить и удаляем
            foreach (Insight insight in Insights)
            {
                if (insight.Id == insightIdNeedToDelete)
                {
                    // если удаляемый элемент находится раньше текущего инсайта в очереди, сдвигаем влево для сохранения порядка
                    if (numberOfDelitedInsightInList < NumberOfLastThought && NumberOfLastThought != 0)
                    {
                        NumberOfLastThought--;
                    }
                    Insights.RemoveAt(numberOfDelitedInsightInList);
                    
                    isDelited = true;
                    break;
                }
                numberOfDelitedInsightInList++;
            }
        }
    }
}
