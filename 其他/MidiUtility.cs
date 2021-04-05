﻿using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Commons.Music.Midi;
using System.IO;

namespace 原神自动弹奏器
{
    public static class MidiUtility
    {
        public static int noteBais = 0;
        public static int octaveBais = 0;
        public static bool isDebugMode = false;
        public static List<NoteScore> notes = new List<NoteScore>();
        public static int standardOctave => notes.Min(note => note.octave) + 1 + octaveBais;
        public class NoteScore
        {
            public int rank;
            public int value;
            public int octave;
            public bool isSharp = false;
            public NoteScore(int rank, string noteName, int octave)
            {

                if (isDebugMode) Console.Write($"检测到音符 编号:{rank}-音度为{octave}-十二律为{noteName}------");
                this.rank = rank;
                switch (noteName)
                {
                    case "C": value = 1; break;
                    //case "CSharp": value = 1; break;
                    case "D": value = 2; break;
                    //case "DSharp": value = 2; break;
                    case "E": value = 3; break;
                    case "F": value = 4; break;
                    //case "FSharp": value = 4; break;
                    case "G": value = 5; break;
                    //case "GSharp": value = 5; break;
                    case "A": value = 6; break;
                    //case "ASharp": value = 6; break;
                    case "B": value = 7; break;
                    default: isSharp = true; break;
                }
                value = value + noteBais;
                if (value < 1)
                {
                    octave--;
                    this.value = value % 7;

                }
                else if (value > 7)
                {
                    octave++;
                    this.value = value % 7;

                }
                this.octave = octave;
                if (isDebugMode) Console.WriteLine($"录入该音符为-音度{octave}-音符{value}");
            }
            public string ToYuanPuNote()
            {
                bool isToNumberPu = false;
                if (octave == standardOctave - 1)
                {
                    switch (value)
                    {
                        case 1: return isToNumberPu ? "1." : "Z";
                        case 2: return isToNumberPu ? "2." : "X";
                        case 3: return isToNumberPu ? "3." : "C";
                        case 4: return isToNumberPu ? "4." : "V";
                        case 5: return isToNumberPu ? "5." : "B";
                        case 6: return isToNumberPu ? "6." : "N";
                        case 7: return isToNumberPu ? "7." : "M";
                        default: return " ";
                    }
                }
                else if (octave == standardOctave)
                {
                    switch (value)
                    {
                        case 1: return isToNumberPu ? "1." : "A";
                        case 2: return isToNumberPu ? "2." : "S";
                        case 3: return isToNumberPu ? "3." : "D";
                        case 4: return isToNumberPu ? "4." : "F";
                        case 5: return isToNumberPu ? "5." : "G";
                        case 6: return isToNumberPu ? "6." : "H";
                        case 7: return isToNumberPu ? "7." : "J";
                        default: return " ";

                    }
                }
                else if (octave == standardOctave + 1)
                {
                    switch (value)
                    {
                        case 1: return isToNumberPu ? "1'" : "Q";
                        case 2: return isToNumberPu ? "2'" : "W";
                        case 3: return isToNumberPu ? "3'" : "E";
                        case 4: return isToNumberPu ? "4'" : "R";
                        case 5: return isToNumberPu ? "5'" : "T";
                        case 6: return isToNumberPu ? "6'" : "Y";
                        case 7: return isToNumberPu ? "7'" : "U";
                        default: return " ";
                    }
                }
                return " ";
            }
        }
        public static void Init(int noteBais, int octaveBais, bool isDebugMode)
        {
            notes.Clear();
            MidiUtility.noteBais = noteBais;
            MidiUtility.octaveBais = octaveBais;
            MidiUtility.isDebugMode = isDebugMode;
        }
        public static void AddNote(NoteScore note)
        {
            if (!note.isSharp)//不加入半音阶
            {
                notes.Add(note);

            }
        }
        public static int GetBPM(FileInfo midnFIle)
        {
            var access = MidiAccessManager.Default;
            var output = access.OpenOutputAsync(access.Outputs.Last().Id).Result;
            var music = MidiMusic.Read(File.OpenRead(midnFIle.FullName));
            var player = new MidiPlayer(music, output);
            int bpm = player.Bpm;
            player.Dispose();
            return bpm;
        }
        public static string TransToYuanShenPu()
        {
            string output = "";
            var tempNotes = notes.Select(note => new { rank = note.rank, note = note.ToYuanPuNote() }).
                GroupBy(note => note.rank).ToList().Select(item =>
                            new
                            {
                                rank = item.Key,
                                notes = string.Join("", item.ToList().Select(x => x.note))
                            }
                         ).ToList();
            if (tempNotes.Any())
            {
                int endRank = tempNotes.Last().rank;
                Enumerable.Range(0, endRank + 1).ToList().ForEach(i =>
                {
                    var note = tempNotes.FirstOrDefault(tempNote => tempNote.rank == i);
                    if (note != null)
                    {
                        output += note.notes.Length > 1 ? $"({note.notes})" : note.notes;
                    }
                    else
                    {
                        output += " ";
                    }
                });
            }

            Console.WriteLine(output); ;
            return output;
        }
        public static string Export(FileInfo midiFileInfo)
        {
            int i = 0;
            string output = "";
            MidiFile midiFile = MidiFile.Read(midiFileInfo.FullName);
            System.Reflection.PropertyInfo propertyInfo = midiFile.TimeDivision.GetType().GetProperty("TicksPerQuarterNote");
            int bpm = int.Parse(propertyInfo.GetValue(midiFile.TimeDivision).ToString());

            foreach (var trackChunk in midiFile.Chunks.OfType<TrackChunk>())
            {

                var list = trackChunk.ManageNotes().Notes.ToList();
                if (list.Any())
                {
                    Console.WriteLine("开始解析音轨" + i);
                    int start = (int)list[0].Time;
                    int templength = ((int)list[0].Length);
                    int length = templength % 120 == 0 ? templength : ((templength / 120) + 1) * 120;
                    List<Note> targetNotes = list.Where(note => note.Channel == 0).ToList();
                    Console.WriteLine("包含的平均十二律为");
                    targetNotes.Select(note => note.NoteName).Distinct().OrderBy(x => x).ToList()
                        .ForEach(noteName => Console.WriteLine(noteName));
                    Console.WriteLine("可能为x调");
                    MidiUtility.notes.Clear();
                    Console.WriteLine("bpm为" + bpm);
                    targetNotes.ForEach(note =>
                    {
                        int rank = (int)(note.Time - start) / bpm;
                        MidiUtility.AddNote(new MidiUtility.NoteScore(rank, note.NoteName.ToString(), note.Octave));
                    });
                    //var s = MidiUtility.notes;
                    string YuanShenPu = MidiUtility.TransToYuanShenPu();
                    if (output == "") output = YuanShenPu;
                    Console.WriteLine("音轨解析完毕");
                }
                i++;
            }
            return output;
        }
    }
}
