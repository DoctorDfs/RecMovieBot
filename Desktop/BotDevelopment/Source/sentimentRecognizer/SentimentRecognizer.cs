
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LuisBot.sentimentRecognizer
{
    public static class SentimentRecognizer
    {
        public static async Task<double?> GetSentiment(string inputText, string analyticsID) {
            // Create a client.

            ITextAnalyticsAPI client = new TextAnalyticsAPI();

            client.AzureRegion = AzureRegions.Westcentralus;
            client.SubscriptionKey = "2763d3dab1404c1e99b75c283f9642b0";

            

            // Extracting language
            LanguageBatchResult resultLanguage = client.DetectLanguage(
                    new BatchInput(
                        new List<Input>()
                        {
                          new Input(analyticsID, inputText),                        
                        }));

            // Printing language results.
            LanguageBatchResultItem docItem = resultLanguage.Documents[0]; // perchè analizzo solo una frase per volta

            string language = string.Empty;
            if (docItem.DetectedLanguages[0].Name.Equals("english"))
                language = "en";

            //Extracting sentiment

            SentimentBatchResult resultSentiment = client.Sentiment(
                    new MultiLanguageBatchInput(
                        new List<MultiLanguageInput>()
                        {
                          new MultiLanguageInput(language, docItem.Id, inputText),
                        }));
    
            return resultSentiment.Documents[0].Score;

        }

    
    }
}