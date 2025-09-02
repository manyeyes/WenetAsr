﻿using WenetAsr.Examples.Utils;

namespace WenetAsr.Examples
{
    internal static partial class Program
    {
        private static WenetAsr.OnlineRecognizer? _onlineRecognizer;
        public static OnlineRecognizer? initOnlineRecognizer(string modelName, string modelBasePath, string modelAccuracy = "int8", int threadsNum = 2)
        {
            if (_onlineRecognizer == null)
            {
                if (string.IsNullOrEmpty(modelName))
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
                    _onlineRecognizer = new OnlineRecognizer(encoderFilePath: encoderFilePath, decoderFilePath: decoderFilePath, ctcFilePath: ctcFilePath, tokensFilePath: tokensFilePath, threadsNum: threadsNum);
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
                    Console.WriteLine($"Error occurred: {ex.Message}");
                }
            }
            return _onlineRecognizer;
        }

        public static void OnlineRecognizer(string streamDecodeMethod = "one", string modelName = "wenet-u2pp-conformer-wenetspeech-onnx-online-20220506", string modelAccuracy = "int8", int threadsNum = 2, string[]? mediaFilePaths = null, string? modelBasePath = null)
        {
            if (string.IsNullOrEmpty(modelBasePath))
            {
                modelBasePath = applicationBase;
            }
            OnlineRecognizer? onlineRecognizer = initOnlineRecognizer(modelName, modelBasePath, modelAccuracy, threadsNum);
            if (onlineRecognizer == null)
            {
                Console.WriteLine("Init models failure!");
                return;
            }
            TimeSpan total_duration = TimeSpan.Zero;
            TimeSpan start_time = TimeSpan.Zero;
            TimeSpan end_time = TimeSpan.Zero;
            start_time = new TimeSpan(DateTime.Now.Ticks);

            List<List<float[]>> samplesList = new List<List<float[]>>();
            int batchSize = 2;
            int startIndex = 0;
            int n = 0;
            List<float[]>? samples = new List<float[]>();
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
                if (n < startIndex)
                {
                    continue;
                }
                if (batchSize <= n - startIndex)
                {
                    break;
                }
                if (!File.Exists(mediaFilePath))
                {
                    continue;
                }
                if (AudioHelper.IsAudioByHeader(mediaFilePath))
                {
                    TimeSpan duration = TimeSpan.Zero;
                    samples = AudioHelper.GetFileChunkSamples(mediaFilePath, ref duration);
                    if (samples.Count > 0)
                    {
                        for (int j = 0; j < 6; j++)
                        {
                            samples.Add(new float[400]);
                        }
                        samplesList.Add(samples);
                        total_duration += duration;
                    }
                }
                n++;
            }
            if (samplesList.Count == 0)
            {
                Console.WriteLine("No media file is read!");
                return;
            }
            streamDecodeMethod = string.IsNullOrEmpty(streamDecodeMethod) ? "multi" : streamDecodeMethod;//one ,multi
            if (streamDecodeMethod == "one")
            {
                //one stream decode
                for (int j = 0; j < samplesList.Count; j++)
                {
                    WenetAsr.OnlineStream stream = onlineRecognizer.CreateOnlineStream();
                    foreach (float[] samplesItem in samplesList[j])
                    {
                        stream.AddSamples(samplesItem);
                        WenetAsr.Model.OnlineRecognizerResultEntity result = onlineRecognizer.GetResult(stream);
                        Console.WriteLine(result.Text);
                    }
                }
                // one stream decode
            }
            //if (streamDecodeMethod == "multi")
            //{
            //    //multi stream decode
            //    List<WenetAsr.OnlineStream> onlineStreams = new List<WenetAsr.OnlineStream>();
            //    List<bool> isEndpoints = new List<bool>();
            //    List<bool> isEnds = new List<bool>();
            //    for (int num = 0; num < samplesList.Count; num++)
            //    {
            //        WenetAsr.OnlineStream stream = onlineRecognizer.CreateOnlineStream();
            //        onlineStreams.Add(stream);
            //        isEndpoints.Add(false);
            //        isEnds.Add(false);
            //    }
            //    int i = 0;
            //    List<WenetAsr.OnlineStream> streams = new List<WenetAsr.OnlineStream>();

            //    while (true)
            //    {
            //        streams = new List<WenetAsr.OnlineStream>();

            //        for (int j = 0; j < samplesList.Count; j++)
            //        {
            //            if (samplesList[j].Count > i && samplesList.Count > j)
            //            {
            //                onlineStreams[j].AddSamples(samplesList[j][i]);
            //                streams.Add(onlineStreams[j]);
            //                isEndpoints[j] = false;
            //            }
            //            else
            //            {
            //                streams.Add(onlineStreams[j]);
            //                samplesList.Remove(samplesList[j]);
            //                isEndpoints[j] = true;
            //            }
            //        }
            //        for (int j = 0; j < samplesList.Count; j++)
            //        {
            //            if (isEndpoints[j])
            //            {
            //                if (onlineStreams[j].IsFinished(isEndpoints[j]))
            //                {
            //                    isEnds[j] = true;
            //                }
            //                else
            //                {
            //                    streams.Add(onlineStreams[j]);
            //                }
            //            }
            //        }
            //        List<WenetAsr.Model.OnlineRecognizerResultEntity> results_batch = onlineRecognizer.GetResults(streams);
            //        foreach (WenetAsr.Model.OnlineRecognizerResultEntity result in results_batch)
            //        {
            //            Console.WriteLine(result.text);
            //        }
            //        Console.WriteLine("");
            //        i++;
            //        bool isAllFinish = true;
            //        for (int j = 0; j < samplesList.Count; j++)
            //        {
            //            if (!isEnds[j])
            //            {
            //                isAllFinish = false;
            //                break;
            //            }
            //        }
            //        if (isAllFinish)
            //        {
            //            break;
            //        }
            //    }
            //}
            if (_onlineRecognizer != null)
            {
                _onlineRecognizer.Dispose();
                _onlineRecognizer = null;
            }
            end_time = new TimeSpan(DateTime.Now.Ticks);
            double elapsed_milliseconds = end_time.TotalMilliseconds - start_time.TotalMilliseconds;
            double rtf = elapsed_milliseconds / total_duration.TotalMilliseconds;
            Console.WriteLine("elapsed_milliseconds:{0}", elapsed_milliseconds.ToString());
            Console.WriteLine("total_duration:{0}", total_duration.TotalMilliseconds.ToString());
            Console.WriteLine("rtf:{1}", "0".ToString(), rtf.ToString());
            Console.WriteLine("Hello, World!");
        }
    }
}
