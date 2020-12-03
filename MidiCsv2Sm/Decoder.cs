using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiCsv2Sm
{
    class Decoder
    {
        private const double tempoPerSecond = 1000000;

        private double ticksPerBeat = 1;

        private double tempo = 1;

        private double currentTime = 0;
        private double lastCurrentTime = 0;
        private double lastTempTicks = 0;
        private int tempSectionNumber = 0;

        private Dictionary<int, ChannelEvent> suspendedChannelEvents = new Dictionary<int, ChannelEvent>();

        public List<ChannelEvent> ChannelEvents = new List<ChannelEvent>();

        public void Parse(string text)
        {
            string[] split = text.Split(',');

            if (split.Length < 2)
                return;

            double tempTicks = Double.Parse(split[1]);
            currentTime = (tempTicks - lastTempTicks) / ticksPerBeat * tempo / tempoPerSecond + lastCurrentTime;

            lastTempTicks = tempTicks;
            lastCurrentTime = currentTime;

            switch (split[2].Trim())
            {
                case "Header":
                    ticksPerBeat = Double.Parse(split[5]);
                    break;

                case "Tempo":
                    if(tempo == -1)
                    {
                        tempo = Double.Parse(split[3]);
                        break;
                    }
                    
                    tempo = Double.Parse(split[3]);
                    break;

                case "Note_on_c":
                    
                    int pitchOn = Int32.Parse(split[4]) - 12;

                    switch (pitchOn)
                    {
                        /* 踏板 */
                        case 11:
                            if (suspendedChannelEvents.ContainsKey(pitchOn))
                            {
                                SustainEvent suspendedSustainEvent = suspendedChannelEvents[pitchOn] as SustainEvent;
                                suspendedSustainEvent.Length = currentTime - suspendedSustainEvent.StartTime - 0.01;
                                ChannelEvents.Add(suspendedSustainEvent);
                                suspendedChannelEvents.Remove(pitchOn);

                                //Console.WriteLine("sustain[" + suspendedSustainEvent.StartTime + "] is Length [" + suspendedSustainEvent.Length + "].");
                            }

                            SustainEvent sustainEvent = new SustainEvent()
                            {
                                StartTime = currentTime,
                                SectionNumber = tempSectionNumber
                            };
                            suspendedChannelEvents.Add(pitchOn, sustainEvent);
                            break;

                        /* 小節 */
                        case 10:
                            if (suspendedChannelEvents.ContainsKey(pitchOn))
                            {
                                SectionEvent suspendedSectionEvent = suspendedChannelEvents[pitchOn] as SectionEvent;
                                suspendedSectionEvent.Length = currentTime - suspendedSectionEvent.StartTime;
                                ChannelEvents.Add(suspendedSectionEvent);
                                suspendedChannelEvents.Remove(pitchOn);
                            }

                            tempSectionNumber++;

                            SectionEvent sectionEvent = new SectionEvent()
                            {
                                StartTime = currentTime,
                                SectionNumber = tempSectionNumber,
                            };
                            suspendedChannelEvents.Add(pitchOn, sectionEvent);
                            break;

                        /* 音符 */
                        default:
                            if (suspendedChannelEvents.ContainsKey(pitchOn))
                            {
                                NoteEvent suspendedNoteEvent = suspendedChannelEvents[pitchOn] as NoteEvent;
                                suspendedNoteEvent.Length = currentTime - suspendedNoteEvent.StartTime;
                                ChannelEvents.Add(suspendedNoteEvent);
                                suspendedChannelEvents.Remove(pitchOn);
                            }

                            NoteEvent noteEvent = new NoteEvent()
                            {
                                Pitch = pitchOn,
                                StartTime = currentTime,
                                Value = Int32.Parse(split[5]),
                                SectionNumber = tempSectionNumber
                            };
                            suspendedChannelEvents.Add(pitchOn, noteEvent);
                            break;

                    }
                    break;

                case "Note_off_c":

                    int pitchOff = Int32.Parse(split[4]) - 12;

                    switch (pitchOff)
                    {
                        /* 踏板 */
                        case 11:
                            if (suspendedChannelEvents.ContainsKey(pitchOff) && false)
                            {
                                SustainEvent suspendedSustainEvent = suspendedChannelEvents[pitchOff] as SustainEvent;
                                suspendedSustainEvent.Length = currentTime - suspendedSustainEvent.StartTime;
                                ChannelEvents.Add(suspendedSustainEvent);
                                suspendedChannelEvents.Remove(pitchOff);
                            }
                            break;

                        /* 小節 */
                        case 10:
                            if (suspendedChannelEvents.ContainsKey(pitchOff) && false)
                            {
                                SectionEvent suspendedSectionEvent = suspendedChannelEvents[pitchOff] as SectionEvent;
                                suspendedSectionEvent.Length = currentTime - suspendedSectionEvent.StartTime;
                                ChannelEvents.Add(suspendedSectionEvent);
                                suspendedChannelEvents.Remove(pitchOff);
                            }
                            break;

                        /* 音符 */
                        default:
                            if (suspendedChannelEvents.ContainsKey(pitchOff))
                            {
                                NoteEvent suspendedNoteEvent = suspendedChannelEvents[pitchOff] as NoteEvent;
                                suspendedNoteEvent.Length = currentTime - suspendedNoteEvent.StartTime;
                                ChannelEvents.Add(suspendedNoteEvent);
                                suspendedChannelEvents.Remove(pitchOff);
                            }
                            break;
                    }
                    break;
            }
        }

        public void CleanSuspendedChannelEvents()
        {

            double endTime = lastCurrentTime + 1;

            foreach (var channelEvent in suspendedChannelEvents)
            {
                switch (channelEvent.Key)
                {
                    /* 踏板 */
                    case 11:
                        SustainEvent suspendedSustainEvent = channelEvent.Value as SustainEvent;
                        suspendedSustainEvent.Length = endTime - suspendedSustainEvent.StartTime;
                        ChannelEvents.Add(suspendedSustainEvent);
                        break;

                    /* 小節 */
                    case 10:
                        SectionEvent suspendedSectionEvent = channelEvent.Value as SectionEvent;
                        suspendedSectionEvent.Length = endTime - suspendedSectionEvent.StartTime;
                        ChannelEvents.Add(suspendedSectionEvent);
                        break;

                    /* 音符 */
                    default:
                        NoteEvent suspendedNoteEvent = channelEvent.Value as NoteEvent;
                        suspendedNoteEvent.Length = endTime - suspendedNoteEvent.StartTime;
                        ChannelEvents.Add(suspendedNoteEvent);
                        break;
                }
            }

            suspendedChannelEvents.Clear();

        }
        
        public void SortChannelEvents()
        {
            ChannelEvents.Sort((x, y) => {

                if (x.StartTime > y.StartTime)
                    return 1;
                else if (x.StartTime < y.StartTime)
                    return -1;
                else if (x.StartTime == y.StartTime)
                {
                    if (x is SectionEvent)
                    {
                        return -1;
                    }
                    else if (y is SectionEvent)
                    {
                        return 1;
                    }

                    if (x is SustainEvent)
                    {
                        return -1;
                    }
                    else if (y is SustainEvent)
                    {
                        return 1;
                    }

                    NoteEvent noteX = x as NoteEvent;
                    NoteEvent noteY = y as NoteEvent;

                    if (noteX.Pitch > noteY.Pitch)
                        return 1;
                    else
                        return -1;

                }

                return 0;
            });
        }
        


        public List<string> Encode()
        {

            List<string> encodedLines = new List<string>();

            foreach (var channelEvent in ChannelEvents)
            {
                string line = "";
                if(channelEvent is NoteEvent)
                {
                    NoteEvent noteEvent = channelEvent as NoteEvent;

                    line = noteEvent.Pitch.ToString() + "," +
                           noteEvent.StartTime.ToString() + "," +
                           noteEvent.Length.ToString() + "," +
                           noteEvent.Value.ToString() + "," +
                           "" + "," +
                           noteEvent.SectionNumber.ToString() + "," +
                           "0";

                }
                else if(channelEvent is SectionEvent)
                {
                    SectionEvent sectionEvent = channelEvent as SectionEvent;

                    line = "-4" + "," +
                           sectionEvent.StartTime.ToString() + "," +
                           sectionEvent.Length.ToString() + "," +
                           "-1" + "," +
                           "0" + "," +
                           sectionEvent.SectionNumber.ToString() + "," +
                           "0";
                }
                else if (channelEvent is SustainEvent)
                {
                    SustainEvent sustainEvent = channelEvent as SustainEvent;

                    line = "-1" + "," +
                           sustainEvent.StartTime.ToString() + "," +
                           sustainEvent.Length.ToString() + "," +
                           "-1" + "," +
                           "5" + "," +
                           sustainEvent.SectionNumber.ToString() + "," +
                           "0";
                }



                encodedLines.Add(line);
            }

            return encodedLines;
        }



    }
}
