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
        private int i = 0;


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
            string[] greetingList = new string[3];
            greetingList[0] = "Bye";
            greetingList[1] = "Bye bye";
            greetingList[2] = "See you soon!";

            Random rnd = new Random();
            await context.PostAsync($"{greetingList[rnd.Next(0, 2)]}, I hope I have been for help! ");

            context.Wait(MessageReceived);
        }


        [LuisIntent("LearningUserPreferences")]
        public async Task LearningUserPreferencesIntent(IDialogContext context, LuisResult result)
        {
            Dictionary<EntityRecommendation, double?> entityScore = new Dictionary<EntityRecommendation, double?>();

            if (firstAcces == true)
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
                        IEnumerator<EntityRecommendation> entity = result.Entities.GetEnumerator();
                        while (entity.MoveNext())
                        {
                            if (enumeratorSentence.Current.Contains(entity.Current.Entity))
                            {
                                entityScore.Add(entity.Current, score);
                            }
                        }
                        entity.Dispose();
                    }
                    enumeratorSentence.Dispose();

                    //insermento delle preferenze  nel db
                    Dictionary<EntityRecommendation, double?>.KeyCollection keys = entityScore.Keys;
                    Dictionary<EntityRecommendation, double?>.KeyCollection.Enumerator key = keys.GetEnumerator();

                    //si da per scontato che il dataset di riconoscimento e quello di inserimento dati siano uguali(cosa normale)
                    DbAccess db = DbAccess.GetInstanceOfDbAccess();
                    db.OpenConnection();

                    bool insert = true;

                    CommandQuery command = CommandQuery.GetInstanceCommandQuery();

                    while (key.MoveNext())
                    {
                        double? p;
                        entityScore.TryGetValue(key.Current, out p);
                        Debug.Print($"{key.Current.Entity} ---- {p}");
                        Debug.Print($"{key.Current.Type}----{key.Current.Entity}");

                        if (key.Current.Type.Equals("movie"))
                        {
                            double? score;
                            entityScore.TryGetValue(key.Current, out score);
                            int rating = 0;
                            if (score > 0.5)
                                rating = 1;

                            Debug.Print($"{key.Current.Entity} ---- {rating}");

                            //insert = command.InsertPreferencesMovie(Convert.ToInt32(command.GetIdUserFromIdChat(convID,db.GetConnection())),Convert.ToInt32(command.GetMovieIdFromName(key.Current.Entity,db.GetConnection())),rating,db.GetConnection());
                        }
                        if (key.Current.Type.Equals("actor"))
                        {
                            double? score;
                            entityScore.TryGetValue(key.Current, out score);
                            int rating = 0;
                            if (score > 0.5)
                                rating = 1;

                            Debug.Print($"{key.Current.Entity} ---- {rating}");

                            //insert = command.InsertPrferencesActor(Convert.ToInt32(command.GetIdUserFromIdChat(convID, db.GetConnection())), Convert.ToInt32(command.GetActorIdFromName(key.Current.Entity,db.GetConnection())),rating,db.GetConnection());

                        }
                        if (key.Current.Type.Equals("director"))
                        {
                            double? score;
                            entityScore.TryGetValue(key.Current, out score);
                            int rating = 0;
                            if (score > 0.5)
                                rating = 1;

                            Debug.Print($"{key.Current.Entity} ---- {rating}");
                            //insert = command.InsertPreferencesDirector(Convert.ToInt32(command.GetIdUserFromIdChat(convID, db.GetConnection())), Convert.ToInt32(command.GetDirectorIdFromName(key.Current.Entity,db.GetConnection())),rating,db.GetConnection());
                        }
                        if (key.Current.Type.Equals("genre"))
                        {
                            double? score;
                            entityScore.TryGetValue(key.Current, out score);
                            int rating = 0;
                            if (score > 0.5)
                                rating = 1;

                            Debug.Print($"{key.Current.Entity} ---- {rating}");
                            //insert = command.InsertPrferencesGenre(Convert.ToInt32(command.GetIdUserFromIdChat(convID, db.GetConnection())), Convert.ToInt32(command.GetGenreIdFromName(key.Current.Entity,db.GetConnection())),rating,db.GetConnection());
                        }
                        key.Dispose();
                    }
                    db.CloseConnection();
                    if (insert)
                        await context.PostAsync($"I understand what you like! Thank you!");
                    else
                        await context.PostAsync($"The preference for this entity is already taken!");

                }
                else
                {
                    await context.PostAsync($"I don't understand who you say! Can you repeat please?");
                }

            }


            context.Wait(MessageReceived);
        }

        [LuisIntent("UserFindFilm")]
        public async Task UserFindFilmIntent(IDialogContext context, LuisResult result)
        {
            if (firstAcces)
            {
                //si avvierà la raccomandazione
                await context.PostAsync($"user find film ");
            }
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
    }
}
