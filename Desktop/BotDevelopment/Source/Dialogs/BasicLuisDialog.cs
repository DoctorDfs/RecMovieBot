using System;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using LuisBot.sentimentAnalyzer;
using LuisBot.ProcessTextRequest;
using System.Diagnostics;
using System.Collections.Generic;
using LuisBot.DatabasesConnection;
using RecommenderRequest;
using LuisBot.CommandPattern;
using System.Linq;

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
        private bool confirmWait = false;
        private bool changePreferenceUnderstand = false;
        
      
        private Dictionary<EntityRecommendation, double?> entityScore = new Dictionary<EntityRecommendation, double?>();
        private Dictionary<EntityRecommendation, List<string>> resultEntity = new Dictionary<EntityRecommendation, List<string>>();
        private Dictionary<EntityRecommendation, List<EntityRecommendation>> entitiesPreferences = new Dictionary<EntityRecommendation, List<EntityRecommendation>>();
        private List<EntityRecommendation> valuetedEntities = new List<EntityRecommendation>();
        private List<string> typeCattured = new List<string>();
        private List<string> changedEntities = new List<string>();

        public BasicLuisDialog(Activity activity) : base(new LuisService(new LuisModelAttribute(
            ConfigurationManager.AppSettings["LuisAppId"],
            ConfigurationManager.AppSettings["LuisAPIKey"],
            domain: ConfigurationManager.AppSettings["LuisAPIHostName"])))
        {
            channel = activity.ChannelId;
            convID = activity.Conversation.Id;
            DbAccess db = DbAccess.GetInstanceOfDbAccess();
            db.OpenConnection();
            CommandQuery command = CommandQuery.GetInstanceCommandQuery();
            if (command.InsertNewUser(convID, db.GetConnection()))
              Debug.Print($"Inserimento dell'utente {convID} andato a buon fine!");
            else
              Debug.Print($"Utente già presente!");
            db.CloseConnection();
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

            if (firstAcces == false && setPreferencesClose == true && confirmWait == false && changePreferenceUnderstand == false)
            {
                await context.PostAsync($"Welcome {convID}! Type - help - for to know how you can use me!");
                
            }
            else
            {
                if(setPreferencesClose == false && confirmWait == false && changePreferenceUnderstand == false)
                    await context.PostAsync($"{greetingList[rnd.Next(0, 2)]} {convID}!");
            }


            if (firstAcces == false)
                firstAcces = true;

            context.Wait(MessageReceived);
        }



        [LuisIntent("GreetingBye")]
        public async Task GreetingByeIntent(IDialogContext context, LuisResult result)
        {
            if (setPreferencesClose == true && confirmWait == false && changePreferenceUnderstand == false)
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
                if (setPreferencesClose == true && confirmWait == false && changePreferenceUnderstand == false)
                {
                    if (result.Entities.Count > 0)
                    {
                        LinkedList<string> detectedSentences = await ProcessText.InvokeRequestResponseService(result.Query);

                        int j = 0;

                        LinkedList<string>.Enumerator enumeratorSentence = detectedSentences.GetEnumerator();

                        while (enumeratorSentence.MoveNext())
                        {
                            double? score = await SentimentAnalyzer.GetSentiment(enumeratorSentence.Current, Convert.ToString(j++));
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
                       
                        Dictionary<EntityRecommendation, double?>.KeyCollection keys = entityScore.Keys;
                        Dictionary<EntityRecommendation, double?>.KeyCollection.Enumerator key = keys.GetEnumerator();

                        //controllo se un entità è di più tipi
                        List<EntityRecommendation> copyEntity = new List<EntityRecommendation>(keys);
                        List<string> examinedKeys = new List<string>();

                        List<EntityRecommendation> resultEntityMultipleType = new List<EntityRecommendation>();
                        try
                        {
                            while (key.MoveNext())
                            {
                                if (!examinedKeys.Contains(key.Current.Entity))
                                {
                                    resultEntityMultipleType = copyEntity.FindAll(x => x.Entity.Equals(key.Current.Entity));
                                    examinedKeys.Add(key.Current.Entity);
                                }
                                List<EntityRecommendation>.Enumerator enu = resultEntityMultipleType.GetEnumerator();

                                if (resultEntityMultipleType.Count == 1)
                                {
                                    if(getSentimentByEntity(key.Current) > 0.5)
                                        await context.PostAsync($"I understand what you like {key.Current.Entity}! Confirm ? Respond with yes or no!");
                                    else
                                        await context.PostAsync($"I understand what you dislike {key.Current.Entity}! Confirm ? Respond with yes or no!");

                                    resultEntityMultipleType.Clear();
                                    resultEntity = null;
                                    confirmWait = true;
                                }

                                if (resultEntityMultipleType.Count > 1)
                                {
                                    string question = string.Empty;
                                    int i = 1;

                                    while (enu.MoveNext())
                                    {
                                        if (i == 1)
                                        {
                                            question = $"{enu.Current.Entity} as ";
                                            if (!entitiesPreferences.ContainsKey(enu.Current))
                                            {
                                                List<EntityRecommendation> copy = new List<EntityRecommendation>(resultEntityMultipleType);
                                                entitiesPreferences.Add(enu.Current, copy);
                                            }
                                        }

                                        if (i < resultEntityMultipleType.Count && i > 0 && !question.Contains(enu.Current.Type))
                                            question += enu.Current.Type + " or ";

                                        if (i == resultEntityMultipleType.Count && !question.Contains(enu.Current.Type))
                                            question += enu.Current.Type;
                                        if (i == resultEntityMultipleType.Count)
                                            question += "?";
                                        i++;
                                    }

                                    await context.PostAsync(question);
                                    setPreferencesClose = false;
                                }

                                List<EntityRecommendation>.Enumerator o = resultEntityMultipleType.GetEnumerator();
                                while (o.MoveNext())
                                {
                                    Debug.Print(o.Current.Entity + " " + o.Current.Type);
                                }
                                resultEntityMultipleType.Clear();
                            }
                           
                        }
                        catch (Exception e) {
                            Debug.Print(e.Message);//questa eccezione mi serve per forzare l'uscita dal ciclo 
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
            if (firstAcces == true)
            {
                if (setPreferencesClose == true && confirmWait == false && changePreferenceUnderstand == false)
                {
                    
                    string movie = await Recommendation.InvokeRequestResponseRecommendationService(convID);
                    if (movie.Length == 0 || movie == null)
                        await context.PostAsync("I did not understand what you like, tell me some other movie that you like!");
                    else
                        await context.PostAsync($"I suggest you: {movie}\nRemember evaluate after see it!");
                }
                else
                {
                    await context.PostAsync("Respond to the all question please!");
                }

            }else
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
        public async Task SelectTypeLikeIntent(IDialogContext context, LuisResult result)
        {

            Dictionary<EntityRecommendation, List<EntityRecommendation>>.Enumerator prefEnum = entitiesPreferences.GetEnumerator();
            if (setPreferencesClose == false && confirmWait == false && changePreferenceUnderstand == false)
            {
                LinkedList<string> detectedSentences = await ProcessText.InvokeRequestResponseService(result.Query);
                try
                {
                                                            
                    if (entitiesPreferences.Count > 1)
                    {
                        bool foundEntity = false;
                        
                        while (prefEnum.MoveNext())
                        {
                            LinkedList<string>.Enumerator sentencesEnumerator = detectedSentences.GetEnumerator();
                            while (sentencesEnumerator.MoveNext())
                            {
                                if (sentencesEnumerator.Current.Contains(prefEnum.Current.Key.Entity))
                                {// se è presente l'entità

                                    foundEntity = true;
                                    resultEntity.Add(prefEnum.Current.Key, new List<string>());
                                    List<EntityRecommendation>.Enumerator typeEnum = prefEnum.Current.Value.GetEnumerator();
                                    bool foundAtMostOneType = false;
                                    
                                    while (typeEnum.MoveNext() && foundAtMostOneType==false)
                                    {

                                        if (sentencesEnumerator.Current.Contains(typeEnum.Current.Type) && !typeCattured.Contains(typeEnum.Current.Type))
                                        {
                                            foundAtMostOneType = true;
                                            typeCattured.Add(typeEnum.Current.Type);
                                            if (!valuetedEntities.Contains(prefEnum.Current.Key))
                                                valuetedEntities.Add(prefEnum.Current.Key);
                                            Dictionary<EntityRecommendation, List<string>>.Enumerator resultEnum = resultEntity.GetEnumerator();
                                            while (resultEnum.MoveNext())
                                            {
                                                if (resultEnum.Current.Key.Equals(prefEnum.Current.Key))
                                                {
                                                    resultEnum.Current.Value.Add(typeEnum.Current.Type);
                                                }
                                            }
                                            resultEnum.Dispose();
                                        }
                                    }
                                    typeEnum.Dispose();
                                    if (foundAtMostOneType == false)
                                        await context.PostAsync($"No type present! Repeat because I need to know please!");
                                }
                            }
                            sentencesEnumerator.Dispose();

                        }
                        typeCattured.Clear();
                        if (foundEntity == false)
                            await context.PostAsync($"No entity is present! Repeat because I need to know!");
                    }
                    
                    if (entitiesPreferences.Count == 1)
                    {
                        while (prefEnum.MoveNext())
                        {
                            resultEntity.Add(prefEnum.Current.Key, new List<string>());
                            List<EntityRecommendation>.Enumerator typeEnum = prefEnum.Current.Value.GetEnumerator();
                            bool found = false;
                            while (typeEnum.MoveNext())
                            {
                                if (result.Query.Contains(typeEnum.Current.Type) && !typeCattured.Contains(typeEnum.Current.Type))
                                {
                                    found = true;
                                    typeCattured.Add(typeEnum.Current.Type);
                                    if (!valuetedEntities.Contains(prefEnum.Current.Key))
                                        valuetedEntities.Add(prefEnum.Current.Key);
                                    Dictionary<EntityRecommendation, List<string>>.Enumerator resultEnum = resultEntity.GetEnumerator();
                                    while (resultEnum.MoveNext())
                                    {
                                        if (resultEnum.Current.Key.Equals(prefEnum.Current.Key))
                                        {
                                            resultEnum.Current.Value.Add(typeEnum.Current.Type);
                                        }
                                    }
                                    resultEnum.Dispose();
                                }
                            }
                            typeEnum.Dispose();
                            if (found == false)
                                await context.PostAsync($"Not type present! Repeat because I need to know please!");
                        }
                        prefEnum.Dispose();
                    }
                }
                catch (Exception e)
                {
                    Debug.Print($"{e.Message}");
                }

                Debug.Print($"entità valutate: {valuetedEntities.Count} == entità da valutare: {entitiesPreferences.Count}");

                if (valuetedEntities.Count > 0)
                {
                    string understandLike = "I understand that you ";
                    Dictionary<EntityRecommendation, List<string>>.Enumerator enumerator = resultEntity.GetEnumerator();
                    int i = 1;
                    while (enumerator.MoveNext())
                    {
                        Debug.Print($"Entità: {enumerator.Current.Key.Entity} con tipi: ");
                        double? like = getSentimentByEntity(enumerator.Current.Key);
                        if (like > 0.5)
                            understandLike += "like ";
                        if (like <= 0.5)
                            understandLike += "dislike ";

                        understandLike += enumerator.Current.Key.Entity + " with type ";
                        List<string>.Enumerator enumerator2 = enumerator.Current.Value.GetEnumerator();

                        while (enumerator2.MoveNext())
                        {
                            understandLike += enumerator2.Current + " ";
                            Debug.Print($"{enumerator2.Current}");
                        }
                        enumerator2.Dispose();
                        Debug.Print("\n\n\n");
                        if (i != resultEntity.Count)
                            understandLike += " and ";
                        i++;
                    }
                    enumerator.Dispose();
                   
                    if (valuetedEntities.Count == entitiesPreferences.Count) // se sono state valutate tutte le entità
                    {

                        await context.PostAsync($"{understandLike}, Confirm ? Reponde with yes or no!");
                        await context.PostAsync("If the type of entity is wrong type 'Type wrong' and repeat again else type 'NO' and i will ask you where i wrong!");

                        setPreferencesClose = true;
                        confirmWait = true;
                    }else
                        await context.PostAsync($"{understandLike}");
                }
               
            }
            else
                await context.PostAsync("Tell the entity what you like with its type one at time! Thank you!");

            context.Wait(MessageReceived);

        }

        [LuisIntent("ConfirmPreferencesIntent")]
        public async Task ConfirmPreferencesIntent(IDialogContext context, LuisResult result)
        {
            if (confirmWait == true && setPreferencesClose == true && changePreferenceUnderstand == false) {

                if (result.Query.ToLower().Contains("yes"))
                {
                    string insert = InsertData.InsertIntoDbVlutation(entityScore, resultEntity, convID);

                    entityScore.Clear();

                    valuetedEntities.Clear();

                    entitiesPreferences.Clear();

                    typeCattured.Clear();

                    if (resultEntity != null)
                        resultEntity.Clear();

                    if (insert.Equals("ok"))
                        await context.PostAsync("Perfect, I Understand!");
                    else
                    {                       
                        await context.PostAsync($"You have already evalueted {insert}");
                    }
                        
                }else
            
                    if (result.Query.ToLower().Contains("type wrong"))
                    {
                        await context.PostAsync("Ok repeat again!");
                        entityScore.Clear();

                        valuetedEntities.Clear();

                        entitiesPreferences.Clear();

                        typeCattured.Clear();

                        if (resultEntity != null)
                            resultEntity.Clear();
                    }else

                        if (result.Query.ToLower().Contains("no"))
                        {

                            Dictionary<EntityRecommendation, double?>.Enumerator e = entityScore.GetEnumerator();
                            string understand = "I understand you ";
                            while (e.MoveNext()) {
                                if (!understand.Contains(e.Current.Key.Entity))
                                {
                                    if (e.Current.Value > 0.5)
                                        understand += "like ";
                                    else
                                        understand += "dislike ";

                                    understand += e.Current.Key.Entity;

                                    await context.PostAsync(understand);

                                    understand = string.Empty;
                                }
                            }

                            changePreferenceUnderstand = true;
                            await context.PostAsync("Tell me what entity's preference wrong typing     'change [name entity]'   one at time please!'");
                        }

                    confirmWait = false;


            } else
                await context.PostAsync(result.Query);

            context.Wait(MessageReceived);

        }

        [LuisIntent("ChangePreferencesIntent")]
        public async Task ChangePreferencesIntent(IDialogContext context, LuisResult result)
        {
            if (changePreferenceUnderstand == true) {
                if (result.Entities.Count > 0)
                {
                    IList<EntityRecommendation> listEntity = result.Entities;

                    IEnumerator<EntityRecommendation> e = listEntity.GetEnumerator();

                 

                    while (e.MoveNext() && !changedEntities.Contains(e.Current.Entity))
                    {


                        bool found = false;

                        Dictionary<EntityRecommendation, List<string>>.KeyCollection.Enumerator r = resultEntity.Keys.GetEnumerator();
                        
                        while (r.MoveNext() && found == false)// se l'entità digitata è presente
                        {
                            if (r.Current.Entity.Equals(e.Current.Entity)) { 
                                found = true;
                            }
                        }

                        if (found == true)
                        {
                            foreach (EntityRecommendation entity in entityScore.Keys.ToList())
                            {
                                if (e.Current.Entity.Equals(entity.Entity))
                                {

                                    Debug.Print(e.Current.Entity + " ----- " + entity.Entity + "con precedente score= {0}", entityScore[entity]);
                                    entityScore[entity] = 1 - entityScore[entity];
                                    Debug.Print("score attuale = {0}", entityScore[entity]);
                                    changedEntities.Add(entity.Entity);
                                }
                            }
                        }
                        else
                            await context.PostAsync($"Entity {e.Current.Entity} is not present in the valutation! Continue with other entities or type 'i finished'.");

                    }
                }
                else
                {
                    if (result.Query.ToLower().Contains("i finished"))
                    {
                        Dictionary<EntityRecommendation, double?>.Enumerator e = entityScore.GetEnumerator();
                        string understand = "I understand you ";
                        while (e.MoveNext())
                        {
                            if (!understand.Contains(e.Current.Key.Entity))
                            {
                                if (e.Current.Value > 0.5)
                                    understand += "like ";
                                else
                                    understand += "dislike ";

                                understand += e.Current.Key.Entity;

                                await context.PostAsync(understand);

                                understand = string.Empty;
                            }


                        }

                        changePreferenceUnderstand = false;
                        confirmWait = true;
                        changedEntities.Clear();
                        await context.PostAsync("Confirm ?");                                      
                    }
                    else
                        await context.PostAsync("I don't understand any entity! ");
                }

                bool equals = true;
               
                Dictionary<EntityRecommendation, List<string>>.KeyCollection.Enumerator re = resultEntity.Keys.GetEnumerator();
               
                while (re.MoveNext() && equals == true)
                {
                    if (!changedEntities.Contains(re.Current.Entity))
                        equals = false;
                }

                if (equals == true)
                {
             
                    await context.PostAsync("You changed all entitie");

                    Dictionary<EntityRecommendation, double?>.Enumerator e = entityScore.GetEnumerator();
                    string understand = "I understand you ";
                    while (e.MoveNext())
                    {
                        if (!understand.Contains(e.Current.Key.Entity))
                        {
                            if (e.Current.Value > 0.5)
                                understand += "like ";
                            else
                                understand += "dislike ";

                            understand += e.Current.Key.Entity;

                            await context.PostAsync(understand);

                            understand = string.Empty;
                        }

                        
                    }

                    changePreferenceUnderstand = false;
                    confirmWait = true;
                    changedEntities.Clear();
                    await context.PostAsync("Confirm ?");

                }
                    

            }
        }

        private double? getSentimentByEntity(EntityRecommendation entity)
        {

            Dictionary<EntityRecommendation, double?>.Enumerator e = entityScore.GetEnumerator();
            while (e.MoveNext())
            {
                if (e.Current.Key.Entity.Equals(entity.Entity))
                {
                    Debug.Print($"{e.Current.Key.Entity} --- {entity.Entity}");
                    Debug.Print($"valore sentiment: {e.Current.Value}");
                    return e.Current.Value;
                }
            }

            return null;

        }

    }
}
