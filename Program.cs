using System;
using System.Linq;
using System.Reactive.Linq;
using Tweetinvi;
using Tweetinvi.Core.Extensions;
using Tweetinvi.Events;
using Tweetinvi.Models;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("You didn't specified tracks to stream from Twitter!");
                return;
            }
            var tracks = args[0].Split(',').ToArray();
            
            var consumerKey         = Environment.GetEnvironmentVariable("TWITTER_CONSUMER_KEY")    ?? args.Skip(1).FirstOrDefault();
            var consumerSecret      = Environment.GetEnvironmentVariable("TWITTER_CONSUMER_SECRET") ?? args.Skip(2).FirstOrDefault();
            var accessToken         = Environment.GetEnvironmentVariable("TWITTER_ACCESS_TOKEN")    ?? args.Skip(3).FirstOrDefault();
            var accessTokenSecret   = Environment.GetEnvironmentVariable("TWITTER_ACCESS_SECRET")   ?? args.Skip(4).FirstOrDefault();
            var credentials = new TwitterCredentials(
                consumerKey,
                consumerSecret,
                accessToken,
                accessTokenSecret
            );

            using (CreateTweetStream(tracks, credentials).Subscribe(
                tweet   => Console.WriteLine(tweet),
                error   => Console.WriteLine($"Twitter stream failed with exception:{Environment.NewLine}{error}"),
                ()      => Console.WriteLine("Twitter stream completed.")
            ))
            {
                Console.WriteLine($"Service started streeming tweets for '{args[0]}'. Press any key to stop.");
                Console.ReadKey();
            }
        }

        private static IObservable<ITweet> CreateTweetStream(string[] tracks, ITwitterCredentials credentials)
        {
            return Observable.Create<ITweet>(observer =>
            {
                var stream = Stream.CreateFilteredStream(credentials);
                tracks.ForEach(track => stream.AddTrack(track));

                var streamClosed = Observable
                    .FromEventPattern<StreamExceptionEventArgs>(
                        handler => stream.StreamStopped += handler,
                        handler => stream.StreamStopped -= handler
                    );
                var sub = Observable
                    .FromEventPattern<MatchedTweetReceivedEventArgs>(
                        handler => stream.MatchingTweetReceived += handler,
                        handler => stream.MatchingTweetReceived -= handler
                    )
                    .Select(x => x.EventArgs.Tweet)
                    .TakeUntil(streamClosed)
                    .Subscribe(observer);
                stream.StartStreamMatchingAnyCondition();
                return sub;
            });
        }
    }
}