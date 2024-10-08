﻿using Homeflix.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Homeflix.BL
{
    public class MovieLibrary
    {
        private const string configFileName = "HomeFlixConfig.json";
        private bool configExists;
        private VlcPlayerConfig playerConfig;

        public bool ConfigExists { 
            get
            {
                return File.Exists(configFileName);
            }           
        }

        public MovieLibrary()
        {            
            LoadConfig();
        }        

        public void InitializeConfig(string vlcLocation) 
        {
            playerConfig = new VlcPlayerConfig();
            playerConfig.VlcPlayerLocation = vlcLocation;
            playerConfig.Movies = new List<Movie>();

            UpdateConfig();
        }       

        public string GetVlcPlayerLocation()
        {
            if (playerConfig != null)
                return playerConfig.VlcPlayerLocation + "\\vlc.exe";

            return "";
        }
        public List<Movie> GetLibraryMovies()
        {
            LoadConfig();
            if (playerConfig?.Movies != null)
                return playerConfig.Movies;

            return null;
        }
        public string GetNextEpisode(Movie movie)
        {
            string[] stringParts = movie.LastEpisodePlayed.Episode.Split(new char[] { 's', 'e' }, StringSplitOptions.RemoveEmptyEntries);
            int seasonIndex = Convert.ToInt32(stringParts[0]) - 1;
            int episodeIndex = Convert.ToInt32(stringParts[1]) - 1;

            if (movie.Seasons.Count > seasonIndex && movie.Seasons[seasonIndex].Episodes.Count > episodeIndex + 1)//another episode exists in current season
            {
                return $"s{stringParts[0]}e{Convert.ToInt32(stringParts[1]) + 1}";
            }

            if (movie.Seasons.Count > seasonIndex + 1)//another season exists
            {
                return $"s{Convert.ToInt32(stringParts[0]) + 1}e1";
            }

            return "s1e1";//start over
        }
        public string GetEpisodeCode(string fileName)
        {
            string episodeCode = "";
            for (int i = 0; i < playerConfig.Movies.Count; i++)            
            {
                Movie movie = playerConfig.Movies[i];
                for (int j = 0; j < movie.Seasons.Count; j++)                
                {
                    Season season = movie.Seasons[j];
                    var epIndex = season.Episodes.FindIndex(e => e.Name == fileName);
                    if (epIndex>-1)
                    {
                        episodeCode = $"s{j + 1}e{epIndex + 1}";
                        return episodeCode;
                    }
                }
            }

            return episodeCode;
        }
        public void RemoveMovieFromLibrary(Movie movie)
        {
            playerConfig?.Movies?.Remove(movie);
            UpdateConfig();
        }
        public void AddMovieToLibrary(string movieSeriesName, string rootFolderPath)
        {
            Movie movie = new Movie();
            movie.Name = movieSeriesName;
            movie.LastEpisodePlayed = null;
            movie.Seasons = new List<Season>();

            //get seasons and episodes
            string[] seasonFolders = Directory.GetDirectories(rootFolderPath);

            // Iterate over each season folder
            int count = 0;
            foreach (string seasonFolder in seasonFolders)
            {
                count++;

                Season season = new Season
                {
                    Name = $"Season {count}",
                    Episodes = new List<Episode>()
                };

                string[] episodeFiles = Directory.GetFiles(seasonFolder, "*.*", SearchOption.TopDirectoryOnly);

                // Iterate over each episode file
                int episodeId = 0;
                foreach (string episodeFile in episodeFiles)
                {
                    // Create a new Episode object
                    Episode episode = new Episode
                    {
                        Id = episodeId,
                        Name = Path.GetFileName(episodeFile), // Use file name as episode name
                        Path = episodeFile // Full path to the episode file
                    };

                    // Add the episode to the season's list of episodes
                    season.Episodes.Add(episode);

                    episodeId++;
                }

                // Add the season to the movie's list of seasons
                movie.Seasons.Add(season);
            }

            int movieId = playerConfig.Movies.Count;
            movie.Id = movieId;

            playerConfig.Movies.Add(movie);
            UpdateConfig();                        
        }
        public void UpdateLastEpisodePlayed(Movie movie)
        {
            Movie selectedMovie = playerConfig.Movies.Find(m => m.Id == movie.Id);

            if (selectedMovie != null)
            {
                selectedMovie.LastEpisodePlayed = movie.LastEpisodePlayed;
                UpdateConfig();
            }
        }

        private void LoadConfig()
        {
            if (File.Exists(configFileName))
            {
                string jsonContent = File.ReadAllText(configFileName);
                playerConfig = JsonConvert.DeserializeObject<VlcPlayerConfig>(jsonContent);
            }
        }
        private void UpdateConfig()
        {
            string jsonString = JsonConvert.SerializeObject(playerConfig);
            File.WriteAllText(configFileName, jsonString);
            LoadConfig();
        }
    }
}
