﻿using WenetAsr.Examples.Utils;
using System.Text;

namespace WenetAsr.Examples
{
    internal static partial class Program
    {
        private static WenetAsr.OfflineRecognizer? _offlineRecognizer;
        public static WenetAsr.OfflineRecognizer InitOfflineRecognizer(string modelName, string modelBasePath, string modelAccuracy = "int8", int threadsNum = 2)
        {
            if (_offlineRecognizer == null)
            {
                if (string.IsNullOrEmpty(modelBasePath) || string.IsNullOrEmpty(modelName))
                {
                    return null;
                }
                string encoderFilePath = applicationBase + "./" + modelName + "/encoder.int8.onnx";
                string decoderFilePath = applicationBase + "./" + modelName + "/decoder.int8.onnx";
                string ctcFilePath = applicationBase + "./" + modelName + "/ctc.int8.onnx";
                string tokensFilePath = applicationBase + "./" + modelName + "/tokens.txt";
                try
                {
                    string folderPath = Path.Combine(modelBasePath, modelName);
                    // 1. Check if the folder exists
                    if (!Directory.Exists(folderPath))
                    {
                        Console.WriteLine($"Error: folder does not exist - {folderPath}");
                        return null;
                    }
                    // 2. Obtain the file names and destination paths of all files
                    // (calculate the paths in advance to avoid duplicate concatenation)
                    var fileInfos = Directory.GetFiles(folderPath)
                        .Select(filePath => new
                        {
                            FileName = Path.GetFileName(filePath),
                            // Recommend using Path. Combine to handle paths (automatically adapt system separators)
                            TargetPath = Path.Combine(modelBasePath, modelName, Path.GetFileName(filePath))
                            // If it is necessary to strictly maintain the original splicing method, it can be replaced with:
                            // TargetPath = $"{modelBasePath}/./{modelName}/{Path.GetFileName(filePath)}"
                        })
                        .ToList();

                    // Process encoder path (priority: containing modelAccuracy>last one that matches prefix)
                    var encoderCandidates = fileInfos
                        .Where(f => f.FileName.StartsWith("encoder."))
                        .ToList();
                    if (encoderCandidates.Any())
                    {
                        // Prioritize selecting files that contain the specified model accuracy
                        var preferredModel = encoderCandidates
                            .LastOrDefault(f => f.FileName.Contains($".{modelAccuracy}."));
                        encoderFilePath = preferredModel?.TargetPath ?? encoderCandidates.Last().TargetPath;
                    }

                    // Process decoder path
                    var decoderCandidates = fileInfos
                        .Where(f => f.FileName.StartsWith("decoder."))
                        .ToList();
                    if (decoderCandidates.Any())
                    {
                        var preferredModeleb = decoderCandidates
                            .LastOrDefault(f => f.FileName.Contains($".{modelAccuracy}."));
                        decoderFilePath = preferredModeleb?.TargetPath ?? decoderCandidates.Last().TargetPath;
                    }

                    // Process ctc path
                    var ctcCandidates = fileInfos
                        .Where(f => f.FileName.StartsWith("ctc."))
                        .ToList();
                    if (ctcCandidates.Any())
                    {
                        var preferredModeleb = ctcCandidates
                            .LastOrDefault(f => f.FileName.Contains($".{modelAccuracy}."));
                        ctcFilePath = preferredModeleb?.TargetPath ?? ctcCandidates.Last().TargetPath;
                    }

                    // Process token paths (take the last one that matches the prefix)
                    tokensFilePath = fileInfos
                        .LastOrDefault(f => f.FileName.StartsWith("tokens") && f.FileName.EndsWith(".txt"))
                        ?.TargetPath ?? "";

                    if (new[] { encoderFilePath, decoderFilePath, ctcFilePath, tokensFilePath }.Any(string.IsNullOrEmpty))
                    {
                        return null;
                    }
                    TimeSpan start_time = new TimeSpan(DateTime.Now.Ticks);
                    _offlineRecognizer = new OfflineRecognizer(encoderFilePath: encoderFilePath, decoderFilePath: decoderFilePath, ctcFilePath: ctcFilePath, tokensFilePath: tokensFilePath, threadsNum: threadsNum);
                    TimeSpan end_time = new TimeSpan(DateTime.Now.Ticks);
                    double elapsed_milliseconds_init = end_time.TotalMilliseconds - start_time.TotalMilliseconds;
                    Console.WriteLine("init_models_elapsed_milliseconds:{0}", elapsed_milliseconds_init.ToString());
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine($"Error: No permission to access this folder");
                }
                catch (PathTooLongException)
                {
                    Console.WriteLine($"Error: File path too long");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error occurred: {ex}");
                }
            }
            return _offlineRecognizer;
        }
        public static void OfflineRecognizer(string streamDecodeMethod = "one", string modelName = "wenet-u2pp-conformer-gigaspeech-onnx-offline-20210728", string modelAccuracy = "int8", int threadsNum = 2, string[]? mediaFilePaths = null, string? modelBasePath = null)
        {
            if (string.IsNullOrEmpty(modelBasePath))
            {
                modelBasePath = applicationBase;
            }
            OfflineRecognizer offlineRecognizer = InitOfflineRecognizer(modelName, modelBasePath, modelAccuracy, threadsNum);
            if (offlineRecognizer == null)
            {
                Console.WriteLine("Init models failure!");
                return;
            }
            TimeSpan total_duration = new TimeSpan(0L);
            List<float[]>? samples = new List<float[]>();
            List<string> paths = new List<string>();
            if (mediaFilePaths == null || mediaFilePaths.Count() == 0)
            {
                //mediaFilePaths = Directory.GetFiles(Path.Combine(modelBasePath, modelName, "test_wavs"));
                string fullPath = Path.Combine(modelBasePath, modelName);
                if (!Directory.Exists(fullPath))
                {
                    mediaFilePaths = Array.Empty<string>(); // 路径不正确时返回空数组
                }
                else
                {
                    mediaFilePaths = Directory.GetFiles(
                        path: fullPath,
                        searchPattern: "*.wav",
                        searchOption: SearchOption.AllDirectories
                    );
                }
            }
            foreach (string mediaFilePath in mediaFilePaths)
            {
                if (!File.Exists(mediaFilePath))
                {
                    continue;
                }
                if (AudioHelper.IsAudioByHeader(mediaFilePath))
                {
                    TimeSpan duration = TimeSpan.Zero;
                    float[]? sample = AudioHelper.GetFileSample(wavFilePath: mediaFilePath, duration: ref duration);
                    if (sample != null)
                    {
                        paths.Add(mediaFilePath);
                        samples.Add(sample);
                        total_duration += duration;
                    }
                }
            }
            if (samples.Count == 0)
            {
                Console.WriteLine("No media file is read!");
                return;
            }
            Console.WriteLine("Automatic speech recognition in progress!");
            TimeSpan start_time = new TimeSpan(DateTime.Now.Ticks);
            streamDecodeMethod = string.IsNullOrEmpty(streamDecodeMethod) ? "multi" : streamDecodeMethod;//one ,multi
            if (streamDecodeMethod == "one")
            {
                // Non batch method
                Console.WriteLine("Recognition results:\r\n");
                try
                {
                    int n = 0;
                    foreach (var sample in samples)
                    {
                        OfflineStream stream = offlineRecognizer.CreateOfflineStream();
                        // Modify the logic here to dynamically modify hot words
                        //stream.Hotwords = Utils.TextHelper.GetHotwords(Path.Combine(modelBasePath, modelName, "tokens.txt"), new string[] {"魔搭" });
                        stream.AddSamples(sample);
                        WenetAsr.Model.OfflineRecognizerResultEntity result = offlineRecognizer.GetResult(stream);
                        Console.WriteLine($"{paths[n]}");
                        StringBuilder r = new StringBuilder();
                        r.Append("{");
                        r.Append($"\"text\": \"{result.Text}\",");
                        r.Append($"\"tokens\":[{string.Join(",", result.Tokens.Select(x => $"\"{x}\"").ToArray())}],");
                        r.Append($"\"timestamps\":[{string.Join(",", result.Timestamps.Select(x => $"[{x.First()},{x.Last()}]").ToArray())}]");
                        r.Append("}");
                        Console.WriteLine($"{r.ToString()}");
                        Console.WriteLine("");
                        n++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.InnerException?.InnerException);
                }
                // Non batch method
            }
            if (streamDecodeMethod == "multi")
            {
                //2. batch method
                Console.WriteLine("Recognition results:\r\n");
                try
                {
                    int n = 0;
                    List<WenetAsr.OfflineStream> streams = new List<WenetAsr.OfflineStream>();
                    foreach (var sample in samples)
                    {
                        WenetAsr.OfflineStream stream = offlineRecognizer.CreateOfflineStream();
                        stream.AddSamples(sample);
                        streams.Add(stream);
                    }
                    List<WenetAsr.Model.OfflineRecognizerResultEntity> results = offlineRecognizer.GetResults(streams);
                    foreach (WenetAsr.Model.OfflineRecognizerResultEntity result in results)
                    {
                        Console.WriteLine($"{paths[n]}");
                        StringBuilder r = new StringBuilder();
                        r.Append("{");
                        r.Append($"\"text\": \"{result.Text}\",");
                        r.Append($"\"tokens\":[{string.Join(",", result.Tokens.Select(x => $"\"{x}\"").ToArray())}],");
                        r.Append($"\"timestamps\":[{string.Join(",", result.Timestamps.Select(x => $"[{x.First()},{x.Last()}]").ToArray())}]");
                        r.Append("}");
                        Console.WriteLine($"{r.ToString()}");
                        Console.WriteLine("");
                        n++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.InnerException?.InnerException.Message);
                }
            }
            if (_offlineRecognizer != null)
            {
                _offlineRecognizer.Dispose();
                _offlineRecognizer = null;
            }
            TimeSpan end_time = new TimeSpan(DateTime.Now.Ticks);
            double elapsed_milliseconds = end_time.TotalMilliseconds - start_time.TotalMilliseconds;
            double rtf = elapsed_milliseconds / total_duration.TotalMilliseconds;
            Console.WriteLine("recognition_elapsed_milliseconds:{0}", elapsed_milliseconds.ToString());
            Console.WriteLine("total_duration_milliseconds:{0}", total_duration.TotalMilliseconds.ToString());
            Console.WriteLine("rtf:{1}", "0".ToString(), rtf.ToString());
            Console.WriteLine("end!");
        }
    }
}
