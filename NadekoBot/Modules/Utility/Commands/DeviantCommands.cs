using Discord;
using Discord.Commands;
using NadekoBot.Classes;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace NadekoBot.Modules.Utility.Commands
{
    class DeviantCommands: DiscordCommand
    {
        public DeviantCommands(DiscordModule module) : base(module)
        {
        }

        internal override void Init(CommandGroupBuilder cgb)
        {
            cgb.CreateCommand(Prefix + "londonNextMeetup").Alias(NadekoBot.BotMention +" when is the next london meetup?", NadekoBot.BotMention + " when is the next London meetup?", NadekoBot.BotMention + " When is the next London meetup?")
                .Description("Have de")
                .Do(e =>
                {
                    var uri = "https://api.meetup.com/London-Deviant-Robot-Comics-movie-games-and-geeks/events?page=1&fields=plain_text_description";
                    postNextEventToChannel(e, uri);
                });
            cgb.CreateCommand(Prefix + "leedsNextMeetup").Alias(NadekoBot.BotMention + " when is the next leeds meetup?", NadekoBot.BotMention + " when is the next Leeds meetup?", NadekoBot.BotMention + " When is the next Leeds meetup?")
           .Description("Is deviant test")
           .Do(e =>
           {
               var uri = "https://api.meetup.com/Leeds-Deviant-Robot-Comics-movies-games-and-geeks/events?page=1&fields=plain_text_description";
               postNextEventToChannel(e, uri);
           });
            cgb.CreateCommand(Prefix + "cambridgeNextMeetup").Alias(NadekoBot.BotMention + " when is the next cambridge meetup?", NadekoBot.BotMention + " when is the next Cambridge meetup?", NadekoBot.BotMention + " When is the next Cambridge meetup?")
           .Description("Is deviant test")
           .Do(e =>
           {
               var uri = "https://api.meetup.com/Cambridge-Deviant-Robot-Comics-movie-games-and-geeks/events?page=1&fields=plain_text_description";
               postNextEventToChannel(e, uri);
           });
        }
        public async void postNextEventToChannel(CommandEventArgs e, string meetupUri)
        {
            var cl = new HttpClient();

            var res = await cl.GetStreamAsync(meetupUri);

            using (var reader = new StreamReader(res))
            {
                while (!reader.EndOfStream)
                {
                    var currentLine = reader.ReadLine();
                    try
                    {


                        var deviantEvents = Newtonsoft.Json.JsonConvert.DeserializeObject<List<MeetupEvent>>(currentLine);
                        deviantEvents.ForEach(deviantEvent =>
                        {

                            var prettyOut = EventToString(deviantEvent);
                            Console.WriteLine(prettyOut);
                            e.Channel.SendMessage("Hi " + e.User.NicknameMention + "\nHere is the next event\n" + prettyOut).ConfigureAwait(false);
                            Thread.Sleep(1000);
                        });
                    }
                    catch (Exception ex)
                    {

                        throw ex;
                    }
                };

            }
        }


        public DateTime FromUnixTime(long unixTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddMilliseconds(unixTime);
        }

        public String EventToString(MeetupEvent deviantEvent)
        {
            StringBuilder eventStringBuilder = new StringBuilder("__**" + deviantEvent.name + "**__\n\n");
            eventStringBuilder.AppendLine("When: " + FromUnixTime(deviantEvent.time).ToString());
            if (deviantEvent.rsvp_limit > 0)
            {
                if (deviantEvent.waitlist_count > 0)
                {
                    eventStringBuilder.AppendLine("__RSVP__: " + deviantEvent.yes_rsvp_count + "/" + deviantEvent.rsvp_limit + " __Waitisted__: " + deviantEvent.waitlist_count);
                }
                else
                {
                    eventStringBuilder.AppendLine("__RSVP__: " + deviantEvent.yes_rsvp_count + "/" + deviantEvent.rsvp_limit);
                }
            }
            eventStringBuilder.AppendLine("__Description__:");
            eventStringBuilder.AppendLine(deviantEvent.plain_text_description);
            if (deviantEvent.how_to_find_us != null)
            {
                eventStringBuilder.AppendLine();
                eventStringBuilder.AppendLine("__How to find us__:");
                eventStringBuilder.AppendLine(deviantEvent.how_to_find_us);
            }
            if (deviantEvent.venue != null)
            {
                eventStringBuilder.AppendLine(VenueToString(deviantEvent.venue));
            }
            return eventStringBuilder.ToString();
        }

        public String VenueToString(Venue venue)
        {

            StringBuilder eventStringBuilder = new StringBuilder("");
            eventStringBuilder.AppendLine();
            eventStringBuilder.AppendLine("__Where to go__:");
            eventStringBuilder.AppendLine(venue.name);
            var address = new List<string>() { venue.address_1, venue.address_2, venue.address_3 }.Distinct();
            address.ForEach(delegate (String line) { eventStringBuilder.AppendLine(line); });
            eventStringBuilder.AppendLine(venue.city);
            return eventStringBuilder.ToString();
        }

    }
    public class MeetupEvent
    {
        public string name { get; set; }
        public string description { get; set; }
        public string how_to_find_us { get; set; }
        public string link { get; set; }
        public string plain_text_description { get; set; }
        public Venue venue { get; set; }
        public long rsvp_limit { get; set; }
        public long yes_rsvp_count { get; set; }
        public long time { get; set; }
        public long waitlist_count { get; set; }
    }

    public class Venue
    {
        public string name { get; set; }
        public float lat { get; set; }
        public float lon { get; set; }
        public string address_1 { get; set; }
        public string address_2 { get; set; }
        public string address_3 { get; set; }
        public string city { get; set; }
        public string localized_country_name { get; set; }
    }
}
