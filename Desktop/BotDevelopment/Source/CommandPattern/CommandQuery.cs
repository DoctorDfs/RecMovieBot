using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web;
using LuisBot.CommandPattern;

namespace LuisBot.CommandPattern
{
    public class CommandQuery
    {
        private static CommandQuery instanceCommand;
        private CommandQuery() { }


        public static CommandQuery GetInstanceCommandQuery() {
            if(instanceCommand == null)
                instanceCommand = new CommandQuery();

            return instanceCommand;
        }
        public bool InsertNewUser(string id_chat, SqlConnection dbReference) {
            
            try
            {
                ReceiverQuery.GenericInsertInto($"Insert into users(id_chat)values('{id_chat}')", dbReference);
                return true;
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                return false;
            } 
        }

        public bool InsertPreferencesMovie(int id_user, int id_movie, int rating, SqlConnection dbReference) {
            try
            {
                ReceiverQuery.GenericInsertInto($"Insert into movie_rating(id_user,id_movie,rating)values('{id_user}',{id_movie},{rating})", dbReference);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public bool InsertPrferencesActor(int id_user, int id_actor, int rating, SqlConnection dbReference) {
            try
            {
                ReceiverQuery.GenericInsertInto($"Insert into actor_rating(id_user,id_actor,rating)values('{id_user}',{id_actor},{rating})", dbReference);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public bool InsertPrferencesGenre(int id_user, int id_genre, int rating, SqlConnection dbReference) {
            try
            {
                ReceiverQuery.GenericInsertInto($"Insert into genre_rating(id_user,id_genre,rating)values('{id_user}',{id_genre},{rating})", dbReference);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public bool InsertPreferencesDirector(int id_user, int id_director, int rating, SqlConnection dbReference) {
            try
            {
                ReceiverQuery.GenericInsertInto($"Insert into director_rating(id_user,id_director,rating)values('{id_user}',{id_director},{rating})", dbReference);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public string GetMovieNameFromId(int id_movie, SqlConnection dbReference) {
            try
            {
                return ReceiverQuery.GenericSelect($"select movie_title from movie where id_movie='{id_movie}'", dbReference);
            }
            catch (Exception e) {
                Debug.Print(e.Message);
                return null;
            }

        }


        public string GetMovieIdFromName(string movieName, SqlConnection dbReference) {
            
            try
            {
                return ReceiverQuery.GenericSelect($"select id_movie from movie where movie_title='{movieName}'", dbReference);
                
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                return null;
            }
            
        }

        public string GetDirectorIdFromName(string directorName, SqlConnection dbReference)
        {
            try
            {
                return ReceiverQuery.GenericSelect($"select id_director from director where director_name='{directorName}'", dbReference);

            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                return null;
            }
        }

        public string GetActorIdFromName(string actorName, SqlConnection dbReference)
        {
            try
            {
                return ReceiverQuery.GenericSelect($"select id_actor from actor where actor_name='{actorName}'", dbReference);

            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                return null;
            }
        }
        public string GetGenreIdFromName(string genreName, SqlConnection dbReference)
        {
            try
            {
                return ReceiverQuery.GenericSelect($"select id_genre from genre where genre_name='{genreName}'", dbReference);

            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                return null;
            }
        }

        public string GetIdUserFromIdChat(string id_chat, SqlConnection dbReference) {
            try
            {
                return ReceiverQuery.GenericSelect($"select id_user from users where id_chat='{id_chat}'", dbReference);

            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                return null;
            }
        }

    }
}