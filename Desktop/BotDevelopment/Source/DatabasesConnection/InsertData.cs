using LuisBot.CommandPattern;
using Microsoft.Bot.Builder.Luis.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LuisBot.DatabasesConnection
{
    public class InsertData
    {

        public static bool InsertIntoDbVlutation(Dictionary<EntityRecommendation, double?> entityScore, Dictionary<EntityRecommendation, List<string>> entityType, string convID)
        {
            DbAccess db = DbAccess.GetInstanceOfDbAccess();
            db.OpenConnection();

            bool insert = true;

            CommandQuery command = CommandQuery.GetInstanceCommandQuery();
            if (entityType != null)
            {
                Dictionary<EntityRecommendation, List<string>>.Enumerator key = entityType.GetEnumerator();

                while (key.MoveNext() && insert == true)
                {
                    Dictionary<EntityRecommendation, double?>.Enumerator entityEnumerator = entityScore.GetEnumerator();
                    List<string> entityConsidered = new List<string>();
                    while (entityEnumerator.MoveNext())
                    {
                        
                        if (key.Current.Key.Entity.Equals(entityEnumerator.Current.Key.Entity) && !entityConsidered.Contains(entityEnumerator.Current.Key.Entity))//quando trovo l'entità
                        {
                            
                            entityConsidered.Add(entityEnumerator.Current.Key.Entity);
                            List<string>.Enumerator enumList = key.Current.Value.GetEnumerator();
                            string type = string.Empty;
                            while (enumList.MoveNext())
                            {
                               
                                int rating = 0;
                                if (entityEnumerator.Current.Value > 0.5)
                                    rating = 1;

                               

                                switch (enumList.Current)
                                {
                                    case "movie": 
                                        insert = command.InsertPreferencesMovie(Convert.ToInt32(command.GetIdUserFromIdChat(convID,db.GetConnection())),Convert.ToInt32(command.GetMovieIdFromName(entityEnumerator.Current.Key.Entity, db.GetConnection())),rating,db.GetConnection());
                                        break;
                                    case "actor": 
                                        insert = command.InsertPrferencesActor(Convert.ToInt32(command.GetIdUserFromIdChat(convID, db.GetConnection())), Convert.ToInt32(command.GetActorIdFromName(entityEnumerator.Current.Key.Entity, db.GetConnection())),rating,db.GetConnection());
                                        break;
                                    case "director": 
                                        insert = command.InsertPreferencesDirector(Convert.ToInt32(command.GetIdUserFromIdChat(convID, db.GetConnection())), Convert.ToInt32(command.GetDirectorIdFromName(entityEnumerator.Current.Key.Entity, db.GetConnection())),rating,db.GetConnection());
                                        break;
                                    case "genre": 
                                        insert = command.InsertPrferencesGenre(Convert.ToInt32(command.GetIdUserFromIdChat(convID, db.GetConnection())), Convert.ToInt32(command.GetGenreIdFromName(entityEnumerator.Current.Key.Entity, db.GetConnection())),rating,db.GetConnection());
                                        break;
                                }                             
                            }
                            enumList.Dispose();
                        }
                    }
                }

                key.Dispose();
            }
            else
            {

                Dictionary<EntityRecommendation, double?>.Enumerator entityEnumerator = entityScore.GetEnumerator();

                while (entityEnumerator.MoveNext())
                {
                    int rating = 0;
                    if (entityEnumerator.Current.Value > 0.5)
                        rating = 1;

                  

                    switch (entityEnumerator.Current.Key.Type)
                    {
                        case "movie":
                            insert = command.InsertPreferencesMovie(Convert.ToInt32(command.GetIdUserFromIdChat(convID,db.GetConnection())),Convert.ToInt32(command.GetMovieIdFromName(entityEnumerator.Current.Key.Entity, db.GetConnection())),rating,db.GetConnection());
                            break;
                        case "actor":
                            insert = command.InsertPrferencesActor(Convert.ToInt32(command.GetIdUserFromIdChat(convID, db.GetConnection())), Convert.ToInt32(command.GetActorIdFromName(entityEnumerator.Current.Key.Entity, db.GetConnection())),rating,db.GetConnection());
                            break;
                        case "director":
                            insert = command.InsertPreferencesDirector(Convert.ToInt32(command.GetIdUserFromIdChat(convID, db.GetConnection())), Convert.ToInt32(command.GetDirectorIdFromName(entityEnumerator.Current.Key.Entity, db.GetConnection())),rating,db.GetConnection());
                            break;
                        case "genre":
                            insert = command.InsertPrferencesGenre(Convert.ToInt32(command.GetIdUserFromIdChat(convID, db.GetConnection())), Convert.ToInt32(command.GetGenreIdFromName(entityEnumerator.Current.Key.Entity, db.GetConnection())),rating,db.GetConnection());
                            break;
                    }

                }
                entityEnumerator.Dispose();
            }

            db.CloseConnection();
            return insert;
        }
    }
}