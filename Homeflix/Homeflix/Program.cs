using Homeflix.BL;
using Homeflix.Models;

CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
MovieLibrary movieLibrary = new MovieLibrary();
MoviePlayer moviePlayer = null;
Movie currentMovie = null;
var vlcClient = new VlcApiClient();
bool vlcCheckStatusRunning = false;

int vlcCommunicationRetryCount = 0;

try
{    
    RunProgram();
}
catch(Exception ex)
{
    Console.WriteLine(ex.Message + ex.StackTrace);

    Console.WriteLine("There was an error please try again...");

    RunProgram();
}

Console.ReadKey();


void RunProgram()
{
    while (true)
    {
        if (!movieLibrary.ConfigExists)
        {
            string vlcPlayerLocation = InterogateUser("Set VLC player folder path where the .exe file is located:", null);
            movieLibrary.InitializeConfig(vlcPlayerLocation);

            moviePlayer = new MoviePlayer(movieLibrary.GetVlcPlayerLocation());
            AddNewMovieToLibrary();
        }
        else
        {
            moviePlayer = new MoviePlayer(movieLibrary.GetVlcPlayerLocation());
            List<Movie> libraryMovies = movieLibrary.GetLibraryMovies();
            ChooseWhatToDo(libraryMovies);
        }
    }
}

void ManageMovie(Movie movie)
{
    vlcCommunicationRetryCount = 0;

    string manageMovieAnswer = InterogateUser($"Play or Remove {movie.Name} from library (P/R):", new List<string> { "P", "R" });
    if (manageMovieAnswer.ToUpper() == "R")
    {
        movieLibrary.RemoveMovieFromLibrary(movie);
    }
    else
    {
        string resumeAnswer = InterogateUser("Resume from where you left off(Y/N):", new List<string> { "Y", "N" });
        if (resumeAnswer.Trim().ToUpper() == "Y")
        {
            //resume play        
            PlayEpisode(movie);
        }
        else
        {
            //play specific episode from begining
            string episodeAnswer = InterogateUser("Which episode do you want to play?(ex:s2e4):", null);
            movie.LastEpisodePlayed = new LastEpisodePlayed { Episode = episodeAnswer, Time = 0 };
            PlayEpisode(movie);
        }
    }
}

void PlayEpisode(Movie movie)
{
    if (movie.LastEpisodePlayed != null)
    {
        string[] stringParts = movie.LastEpisodePlayed.Episode.Split(new char[] { 's', 'e' }, StringSplitOptions.RemoveEmptyEntries);
        int seasonIndex = Convert.ToInt32(stringParts[0]) - 1;
        int episodeIndex = Convert.ToInt32(stringParts[1]) - 1;

        if (movie.Seasons.Count > seasonIndex && movie.Seasons[seasonIndex].Episodes.Count > episodeIndex)
        {
            Console.WriteLine($"Playing {movie.Name} {movie.Seasons[seasonIndex].Name} {movie.Seasons[seasonIndex].Episodes[episodeIndex].Name}");
            moviePlayer.Play(movie.Seasons[seasonIndex].Episodes[episodeIndex].Path, movie.LastEpisodePlayed.Time);

            Thread.Sleep(1000);
            StartPeriodicVlcStatusCheck();
        }
        else
        {
            Console.WriteLine("Episode does not exist");            
            StopPeriodicTask();

            //reload movies with correct lastEpisodePlayed
            List<Movie> libraryMovies = movieLibrary.GetLibraryMovies();
            ChooseWhatToDo(libraryMovies);
        }
    }
    else
    {
        movie.LastEpisodePlayed = new LastEpisodePlayed() { Episode = "s1e1", Time = 0 };
        Console.WriteLine($"Playing {movie.Name} {movie.Seasons[0].Name} {movie.Seasons[0].Episodes[0].Name}");
        moviePlayer.Play(movie.Seasons[0].Episodes[0].Path, 0);
        Thread.Sleep(1000);
        StartPeriodicVlcStatusCheck();
    }

    currentMovie = movie;
}

void ChooseWhatToDo(List<Movie> libraryMovies = null)
{
    if (libraryMovies == null)
        libraryMovies = movieLibrary.GetLibraryMovies();

    string moviesQuestion = "What do you want to do?\n0 --- add new movie series to library\n";

    int count = 0;
    foreach (Movie movie in libraryMovies)
    {
        count++;
        string line = $"{count} --- {movie.Name}\n";
        moviesQuestion += line;
    }
    List<string> validAnswers = Enumerable.Range(0, libraryMovies.Count + 1).Select(x => x.ToString()).ToList();

    string answer = InterogateUser(moviesQuestion, validAnswers);    

    if (answer == "0")
        AddNewMovieToLibrary();
    else
        ManageMovie(libraryMovies[Convert.ToInt32(answer) - 1]);

    while (vlcCheckStatusRunning)
    {
        if (vlcCommunicationRetryCount > 0)
            Console.WriteLine("Vlc api not responding...");

        Thread.Sleep(2000);
    }
}

void AddNewMovieToLibrary()
{
    string addNewMovie = InterogateUser("Add new movie series to library?(Y/N):", new List<string> { "Y", "N" });
    while (addNewMovie.ToUpper() == "Y")
    {
        string movieRootFolder = InterogateUser("Set root path of the series folder:", null);
        string movieName = InterogateUser("Set Name of the movie series :", null);
        movieLibrary.AddMovieToLibrary(movieName, movieRootFolder);

        addNewMovie = InterogateUser("Add new movie series folder?(Y/N):", new List<string> { "Y", "N" });
    }


    List<Movie> libraryMovies = movieLibrary.GetLibraryMovies();
    ChooseWhatToDo(libraryMovies);    
}

string InterogateUser(string question, List<string> validAnswers)
{
    string response = "";
    if (validAnswers != null)
    {
        while (!validAnswers.Contains(response?.Trim().ToUpper()))
        {
            Console.Write(question);
            response = Console.ReadLine().Trim();
        }
    }
    else
    {
        Console.Write(question);
        response = Console.ReadLine().Trim();
    }

    return response;
}

async Task ManageCurrentPlayback()
{
    //call vlc api and get current time and update currentmovie.lastepisodeplayed time
    //also check if it needs to jump to the next episode(stop player and restart it with the new episode and update the currentmovie.lastepisodeplayed)
    var (currentTime, maxLength, fileNamePlaying) = await vlcClient.GetPlaybackStatusAsync();

    if (currentTime == null)
    {
        vlcCommunicationRetryCount++;       

        if(vlcCommunicationRetryCount > 1)
        {            
            StopPeriodicTask();            
        }
    }
    else
    {
        //reset
        vlcCommunicationRetryCount = 0;

        if (currentTime > maxLength - 20)//20 seconds left jump to next episode
        {
            moviePlayer.Stop();
            string episodeCode = movieLibrary.GetNextEpisode(currentMovie);
            LastEpisodePlayed lastEpisodePlayed = new LastEpisodePlayed() { Episode = episodeCode, Time = 0 };
            currentMovie.LastEpisodePlayed = lastEpisodePlayed;
            movieLibrary.UpdateLastEpisodePlayed(currentMovie);
            PlayEpisode(currentMovie);
        }
        else
        {
            string episodeCodePlaying = movieLibrary.GetEpisodeCode(fileNamePlaying);
            if (episodeCodePlaying.Trim() != "")
                currentMovie.LastEpisodePlayed.Episode = episodeCodePlaying;

            currentMovie.LastEpisodePlayed.Time = currentTime.Value;
            movieLibrary.UpdateLastEpisodePlayed(currentMovie);
        }
    }
}

async Task StartPeriodicVlcStatusCheck()
{
    cancellationTokenSource = new CancellationTokenSource();
    CancellationToken token = cancellationTokenSource.Token;

    vlcCheckStatusRunning = true;
    try
    {
        while (!token.IsCancellationRequested)
        {
            await ManageCurrentPlayback();            
            // Wait for 5 seconds
            await Task.Delay(TimeSpan.FromSeconds(2), token);
        }
    }
    catch (TaskCanceledException)
    {
        Console.WriteLine("Periodic vlc status check was canceled.");
    }
    finally
    {
        vlcCheckStatusRunning = false;
    }
}

void StopPeriodicTask()
{
    cancellationTokenSource?.Cancel();    
}


