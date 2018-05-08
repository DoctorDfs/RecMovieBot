using System;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using LuisBot.sentimentRecognizer;
using LuisBot.ProcessTextRequest;
using System.Diagnostics;
using System.Collections.Generic;
using LuisBot.DatabasesConnection;
using LuisBot.CommandPattern;

namespace Microsoft.Bot.Sample.LuisBot
{

    [Serializable]
    public class BasicLuisDialog : LuisDialog<object>
    {
        private bool firstAcces = false;
        private string username = string.Empty;
        private string convID = string.Empty;
        private string channel = string.Empty;
        private bool setPreferencesClose = true;
      
        private Dictionary<EntityRecommendation, double?> entityScore = new Dictionary<EntityRecommendation, double?>();
        private Dictionary<EntityRecommendation, List<EntityRecommendation>> entitiesPreferences = new Dictionary<EntityRecommendation, List<EntityRecommendation>>();
        private List<EntityRecommendation> valuetedEntities = new List<EntityRecommendation>();

        public BasicLuisDialog(Activity activity) : base(new LuisService(new LuisModelAttribute(
            ConfigurationManager.AppSettings["LuisAppId"],
            ConfigurationManager.AppSettings["LuisAPIKey"],
            domain: ConfigurationManager.AppSettings["LuisAPIHostName"])))
        {
            channel = activity.ChannelId;
            convID = activity.Conversation.Id;
            //DbAccess db = DbAccess.GetInstanceOfDbAccess();
            //db.OpenConnection();
            //CommandQuery command = CommandQuery.GetInstanceCommandQuery();
            //if (command.InsertNewUser(convID, db.GetConnection()))
            //  Debug.Print($"Inserimento dell'utente {convID} andato a buon fine!");
            //else
            //  Debug.Print($"Utente già presente!");
            //db.CloseConnection();
        }

        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"I don't understand, can you repeat please ?");

            context.Wait(MessageReceived);

        }

        [LuisIntent("Greeting")]
        public async Task GreetingIntent(IDialogContext context, LuisResult result)
        {
            string[] greetingList = new string[3];
            greetingList[0] = "Hi";
            greetingList[1] = "Hello";
            greetingList[2] = "Nice to meet you!";

            Random rnd = new Random();

            if (firstAcces == false)
            {
                await context.PostAsync($"Welcome {convID}! Type - help - for to know how you can use me!");
                //identify = true; solo se si considera la modalit? d avvio /start per telegram e non per gli altri canali
            }
            else
            {
                await context.PostAsync($"{greetingList[rnd.Next(0, 2)]} {convID}!");
            }


            if (firstAcces == false)
                firstAcces = true;

            context.Wait(MessageReceived);
        }



        [LuisIntent("GreetingBye")]
        public async Task GreetingByeIntent(IDialogContext context, LuisResult result)
        {
            if (setPreferencesClose == true)
            {
                string[] greetingList = new string[3];
                greetingList[0] = "Bye";
                greetingList[1] = "Bye bye";
                greetingList[2] = "See you soon!";

                Random rnd = new Random();
                await context.PostAsync($"{greetingList[rnd.Next(0, 2)]}, I hope I have been for help! ");
            }else
                await context.PostAsync("Respond to the question please!");
            context.Wait(MessageReceived);
        }


        [LuisIntent("LearningUserPreference")]
        public async Task LearningUserPreferencesIntent(IDialogContext context, LuisResult result)
        {
            
          

            if (firstAcces == true)
            {
                if (setPreferencesClose == true)
                {
                    if (result.Entities.Count > 0)
                    {
                        LinkedList<string> detectedSentences = await ProcessText.InvokeRequestResponseService(result.Query);

                        int j = 0;

                        LinkedList<string>.Enumerator enumeratorSentence = detectedSentences.GetEnumerator();

                        while (enumeratorSentence.MoveNext())
                        {
                            double? score = await SentimentRecognizer.GetSentiment(enumeratorSentence.Current, Convert.ToString(j++));
                            //per ogni entità controllo se sia presente in questa frase
                            IEnumerator<EntityRecommendation> e = result.Entities.GetEnumerator();
                            while (e.MoveNext())
                            {
                                if (enumeratorSentence.Current.Contains(e.Current.Entity))
                                {
                                    entityScore.Add(e.Current, score);
                                }
                            }
                            e.Dispose();
                        }
                        enumeratorSentence.Dispose();

                        //insermento delle preferenze  nel db
                        Dictionary<EntityRecommendation, double?>.KeyCollection keys = entityScore.Keys;
                        Dictionary<EntityRecommendation, double?>.KeyCollection.Enumerator key = keys.GetEnumerator();

                        //controllo se un entità è di più tipi
                        List<EntityRecommendation> copyEntity = new List<EntityRecommendation>(keys);
                        List<string> examinedKeys = new List<string>();

                        List<EntityRecommendation> resultEntity = new List<EntityRecommendation>();

                        while (key.MoveNext())
                        {

                            if (!examinedKeys.Contains(key.Current.Entity))
                            {
                                resultEntity = copyEntity.FindAll(x => x.Entity.Equals(key.Current.Entity));
                            }
                            examinedKeys.Add(key.Current.Entity);

                            List<EntityRecommendation>.Enumerator enu = resultEntity.GetEnumerator();

                            if (resultEntity.Count == 1)
                            {
                                //set preferences
                                Debug.Print("non ci sono entità con doppio tipo");
                                double? p;
                                entityScore.TryGetValue(key.Current, out p);
                                Debug.Print($"{key.Current.Entity} with sentiment {p}");

                                await context.PostAsync($"I understand what you like {key.Current.Entity}! Thank you!");
                                entityScore.Clear();
                                valuetedEntities.Clear();
                                entitiesPreferences.Clear();
                            }
                          
                                
                            if (resultEntity.Count > 1)
                            {
                                Debug.Print("entità con doppio tipo");
                                string question = string.Empty;
                                int i = 1;

                                while (enu.MoveNext())
                                {
                                    if (i == 1) { 
                                        question = $"{enu.Current.Entity} like ";
                                        entitiesPreferences.Add(enu.Current, resultEntity);
                                    }

                                    if (i < resultEntity.Count && i > 0)
                                        question += enu.Current.Type + " or ";

                                    if (i == resultEntity.Count)
                                        question += enu.Current.Type + " ? ";
                                    i++;
                                }
                                   
                                await context.PostAsync(question);
                                setPreferencesClose = false;
                            }
                            

                            resultEntity.Clear();                      
                        }
                    }
                    else
                    {
                        await context.PostAsync("I don't understand who you say! Can you repeat please?");
                    }
                }else
                    await context.PostAsync("Respond to the all question please");

            }
            else
                await context.PostAsync("Rude! Say hello!");


            context.Wait(MessageReceived);
        }

        [LuisIntent("UserFindFilm")]
        public async Task UserFindFilmIntent(IDialogContext context, LuisResult result)
        {
            if (firstAcces)
            {
                if (setPreferencesClose)
                {
                    //si avvierà la raccomandazione
                    await context.PostAsync($"user find film ");
                }
                else
                {
                    await context.PostAsync("Respond to the all question please!");
                }

            }
            await context.PostAsync("Rude! Say Hello!");
            context.Wait(MessageReceived);
        }

        [LuisIntent("Help")]
        public async Task HelpIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"hello," +
                                    $"for a recommendation let me know what you like, " +
                                    $"- for example I like the actor [actor name] - " +
                                    $"- I like the genre [genre name] - " +
                                    $"- I like the director [director name] " +
                                    $" and what you don't like,  " +
                                    $"- for example, I do not like the genre [genre name] " +
                                    $"- I don't like the actor [actor name] " +
                                    $"- I don't like the director [director name] . " +
                                    $"For to have a raccomandation say me for example - Can you find a film for me please ? - " +
                                    $"- Can you recommend me a film please ? -. " +
                                    $"" +
                                    $"\nRemember to always be kind and to greet as soon as you arrive and when you go away!");
            context.Wait(MessageReceived);
        }

        [LuisIntent("SelectTypeLike")]
        public async Task SelectTypeLikeIntent(IDialogContext context, LuisResult result) {
            Debug.Print($"{result.Query}");
            Dictionary<EntityRecommendation, List<EntityRecommendation>>.Enumerator entity = entitiesPreferences.GetEnumerator();

            Dictionary<EntityRecommendation, List<EntityRecommendation>>.KeyCollection keys = entitiesPreferences.Keys;
            Dictionary<EntityRecommendation, List<EntityRecommendation>>.KeyCollection.Enumerator keyEnumerator = keys.GetEnumerator();
            if (setPreferencesClose == false)
            {
                if (entitiesPreferences.Count > 1)
                {
                    while (keyEnumerator.MoveNext())
                    {
                        Debug.Print($"{keyEnumerator.Current.Entity}");
                        IEnumerator<EntityRecommendation> resEnumerator = result.Entities.GetEnumerator();// si suppone che ci sarà sempre e sola una entità

                        while (resEnumerator.MoveNext())
                        {
                            Debug.Print($"{resEnumerator.Current.Entity}{keyEnumerator.Current.Entity}");
                            if (resEnumerator.Current.Entity.Equals(keyEnumerator.Current.Entity) && !valuetedEntities.Contains(keyEnumerator.Current))
                            {
                                
                                //get type
                                List<EntityRecommendation> entityAndMultipleTypeList;

                                entitiesPreferences.TryGetValue(keyEnumerator.Current, out entityAndMultipleTypeList);

                                List<EntityRecommendation>.Enumerator entityAndTypeEnumerator = entityAndMultipleTypeList.GetEnumerator();
                                Dictionary<EntityRecommendation, string> entityType = new Dictionary<EntityRecommendation, string>();
                                while (entityAndTypeEnumerator.MoveNext())
                                {
                                    if (result.Query.ToLower().Contains(entityAndTypeEnumerator.Current.Type.ToLower()))
                                    {
                                        entityType.Add(keyEnumerator.Current, entityAndTypeEnumerator.Current.Type.ToLower());
                                    }
                                }

                                //set preferences
                                Debug.Print("Più entità multiple");
                                Dictionary<EntityRecommendation, string>.Enumerator t = entityType.GetEnumerator();
                                while (t.MoveNext())
                                {
                                    Debug.Print($"{t.Current.Key} with type {t.Current.Value}");
                                }

                                valuetedEntities.Add(keyEnumerator.Current);
                            }

                        }
                    }
                }
                else
                {

                    //get type
                    List<EntityRecommendation> entityAndMultipleTypeList;

                    entitiesPreferences.TryGetValue(keyEnumerator.Current, out entityAndMultipleTypeList);

                    List<EntityRecommendation>.Enumerator entityAndTypeEnumerator = entityAndMultipleTypeList.GetEnumerator();
                    Dictionary<EntityRecommendation, string> entityType = new Dictionary<EntityRecommendation, string>();
                    while (entityAndTypeEnumerator.MoveNext())
                    {
                        if (result.Query.ToLower().Contains(entityAndTypeEnumerator.Current.Type.ToLower()))
                        {
                            entityType.Add(keyEnumerator.Current, entityAndTypeEnumerator.Current.Type.ToLower());
                        }
                    }

                    //set preferences
                    Debug.Print("Solo un entità multipla");
                    Dictionary<EntityRecommendation, string>.Enumerator t = entityType.GetEnumerator();
                    while (t.MoveNext()) {
                        Debug.Print($"{t.Current.Key} with type {t.Current.Value}");
                    }

                    valuetedEntities.Add(keyEnumerator.Current);
                }

                
                if (valuetedEntities.Count == entitiesPreferences.Count)
                {
                    setPreferencesClose = true;
                    entityScore.Clear();
                    valuetedEntities.Clear();
                    entitiesPreferences.Clear();
                    await context.PostAsync($"I understand what you like thank you!");
                }
            }
        }

        private void SetPreferences(LuisResult result) {

            

        }
        
 
    }
}
