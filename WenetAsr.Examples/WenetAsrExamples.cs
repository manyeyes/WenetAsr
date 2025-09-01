﻿using WenetAsr.Examples.Utils;

namespace WenetAsr.Examples
{
    internal static partial class Program
    {
        public static WenetAsr.OfflineRecognizer initWenetAsrOfflineRecognizer(string modelName)
        {
            string encoderFilePath = applicationBase + "./" + modelName + "/encoder.int8.onnx";
            string decoderFilePath = applicationBase + "./" + modelName + "/decoder.int8.onnx";
            string ctcFilePath = applicationBase + "./" + modelName + "/ctc.int8.onnx";
            string tokensFilePath = applicationBase + "./" + modelName + "/tokens.txt";
            WenetAsr.OfflineRecognizer offlineRecognizer = new WenetAsr.OfflineRecognizer(encoderFilePath, decoderFilePath, ctcFilePath, tokensFilePath, threadsNum: 1);
            return offlineRecognizer;
        }

        public static void test_WenetAsrOfflineRecognizer(List<float[]>? samples = null)
        {
            //string modelName = "wenet_u2pp_conformer_aishell_onnx_offline_20211025";
            string modelName = "wenet_u2pp_conformer_gigaspeech_onnx_offline_20210728";
            //string modelName = "wenet_u2pp_conformer_wenetspeech_onnx_offline_20220506";
            //string modelName = "wenet_u2pp_conformer_aishell_onnx_offline_20210601";
            //string modelName = "wenet_u2pp_conformer_aishell2_onnx_offline_20210618";
            WenetAsr.OfflineRecognizer offlineRecognizer = initWenetAsrOfflineRecognizer(modelName);
            TimeSpan total_duration = new TimeSpan(0L);
            List<List<float[]>> samplesList = new List<List<float[]>>();
            if (samples == null)
            {
                samples = new List<float[]>();
                for (int i = 0; i < 4; i++)
                {
                    string wavFilePath = string.Format(applicationBase + "./" + modelName + "/test_wavs/{0}.wav", i.ToString());
                    if (!File.Exists(wavFilePath))
                    {
                        continue;
                    }
                    // method 1
                    //TimeSpan duration = TimeSpan.Zero;
                    //float[] sample = SpeechProcessing.AudioHelper.GetFileSample(wavFilePath, ref duration);
                    //samples.Add(sample);
                    //total_duration += duration;
                    //method 2
                    TimeSpan duration = TimeSpan.Zero;
                    samples = AudioHelper.GetFileChunkSamples(wavFilePath, ref duration);
                    samplesList.Add(samples);
                    total_duration += duration;
                }
            }
            else
            {
                samplesList.Add(samples);
            }
            TimeSpan start_time = new TimeSpan(DateTime.Now.Ticks);
            List<WenetAsr.OfflineStream> streams = new List<WenetAsr.OfflineStream>();
            // method 1
            //foreach (var sample in samples)
            //{
            //    OfflineStream stream = offlineRecognizer.CreateOfflineStream();
            //    stream.AddSamples(sample);
            //    streams.Add(stream);
            //}
            // method 2
            foreach (List<float[]> samplesListItem in samplesList)
            {
                WenetAsr.OfflineStream stream = offlineRecognizer.CreateOfflineStream();
                foreach (float[] sample in samplesListItem)
                {
                    stream.AddSamples(sample);
                }
                streams.Add(stream);
            }
            // decode,fit batch=1
            foreach (WenetAsr.OfflineStream stream in streams)
            {
                WenetAsr.Model.OfflineRecognizerResultEntity result = offlineRecognizer.GetResult(stream);
                Console.WriteLine(result.Text);
                Console.WriteLine("");
            }
            //fit batch>1,but all in one
            //List<WenetAsr.Model.OfflineRecognizerResultEntity> results_batch = offlineRecognizer.GetResults(streams);
            //foreach (WenetAsr.Model.OfflineRecognizerResultEntity result in results_batch)
            //{
            //    Console.WriteLine(result.Text);
            //    Console.WriteLine("");
            //}
            TimeSpan end_time = new TimeSpan(DateTime.Now.Ticks);
            double elapsed_milliseconds = end_time.TotalMilliseconds - start_time.TotalMilliseconds;
            double rtf = elapsed_milliseconds / total_duration.TotalMilliseconds;
            Console.WriteLine("elapsed_milliseconds:{0}", elapsed_milliseconds.ToString());
            Console.WriteLine("total_duration:{0}", total_duration.TotalMilliseconds.ToString());
            Console.WriteLine("rtf:{1}", "0".ToString(), rtf.ToString());
            Console.WriteLine("Hello, World!");
        }

        public static WenetAsr.OnlineRecognizer initWenetAsrOnlineRecognizer(string modelName)
        {
            string encoderFilePath = applicationBase + "./" + modelName + "/encoder.int8.onnx";
            string decoderFilePath = applicationBase + "./" + modelName + "/decoder.int8.onnx";
            string ctcFilePath = applicationBase + "./" + modelName + "/ctc.int8.onnx";
            //string configFilePath = applicationBase + "./" + modelName + "/asr.yaml";
            string tokensFilePath = applicationBase + "./" + modelName + "/tokens.txt";
            WenetAsr.OnlineRecognizer onlineRecognizer = new WenetAsr.OnlineRecognizer(encoderFilePath, decoderFilePath, ctcFilePath, tokensFilePath);
            return onlineRecognizer;
        }

        public static void test_WenetAsrOnlineRecognizer(List<float[]>? samples = null)
        {
            //string modelName = "wenet-u2pp-conformer-aishell-onnx-online-20211025";
            //string modelName = "wenet-u2pp-conformer-gigaspeech-onnx-online-20210728";
            string modelName = "wenet-u2pp-conformer-wenetspeech-onnx-online-20220506";
            //string modelName = "wenet-u2pp-conformer-aishell-onnx-online-20210601";
            //string modelName = "wenet-u2pp-conformer-aishell2-onnx-online-20210618";
            WenetAsr.OnlineRecognizer onlineRecognizer = initWenetAsrOnlineRecognizer(modelName);
            TimeSpan total_duration = TimeSpan.Zero;
            TimeSpan start_time = TimeSpan.Zero;
            TimeSpan end_time = TimeSpan.Zero;


            List<List<float[]>> samplesList = new List<List<float[]>>();
            int batchSize = 1;
            int startIndex = 2;
            if (samples == null)
            {
                samples = new List<float[]>();
                for (int n = startIndex; n < startIndex + batchSize; n++)
                {
                    string wavFilePath = string.Format(applicationBase + "./" + modelName + "/test_wavs/{0}.wav", n.ToString());
                    if (!File.Exists(wavFilePath))
                    {
                        continue;
                    }
                    // method 1
                    TimeSpan duration = TimeSpan.Zero;
                    samples = Utils.AudioHelper.GetFileChunkSamples(wavFilePath, ref duration, chunkSize: 160 * 6);
                    for (int j = 0; j < 30; j++)
                    {
                        samples.Add(new float[400]);
                    }
                    samplesList.Add(samples);
                    total_duration += duration;
                    // method 2
                    //List<TimeSpan> durations = new List<TimeSpan>();
                    //samples = SpeechProcessing.AudioHelper.GetMediaChunkSamples(wavFilePath, ref durations);
                    //samplesList.Add(samples);
                    //foreach(TimeSpan duration in durations)
                    //{
                    //    total_duration += duration;
                    //}
                }
            }
            else
            {
                samplesList.Add(samples);
            }
            start_time = new TimeSpan(DateTime.Now.Ticks);
            // one stream decode
            //for (int j = 0; j < samplesList.Count; j++)
            //{
            //    K2TransducerAsr.OnlineStream stream = onlineRecognizer.CreateOnlineStream();//new K2TransducerAsr.OnlineStream(16000,80);
            //    foreach (float[] samplesItem in samplesList[j])
            //    {
            //        stream.AddSamples(samplesItem);
            //    }
            //    // 1
            //    int w = 0;
            //    while (w < 17)
            //    {
            //        OnlineRecognizerResultEntity result_on = onlineRecognizer.GetResult(stream);
            //        Console.WriteLine(result_on.text);
            //        w++;
            //    }
            //    // 2
            //    //OnlineRecognizerResultEntity result_on = onlineRecognizer.GetResult(stream);
            //    //Console.WriteLine(result_on.text);
            //}

            //multi streams decode
            List<WenetAsr.OnlineStream> onlineStreams = new List<WenetAsr.OnlineStream>();
            List<bool> isEndpoints = new List<bool>();
            List<bool> isEnds = new List<bool>();
            for (int num = 0; num < samplesList.Count; num++)
            {
                WenetAsr.OnlineStream stream = onlineRecognizer.CreateOnlineStream();
                onlineStreams.Add(stream);
                isEndpoints.Add(false);
                isEnds.Add(false);
            }
            int i = 0;
            List<WenetAsr.OnlineStream> streams = new List<WenetAsr.OnlineStream>();

            while (true)
            {
                streams = new List<WenetAsr.OnlineStream>();

                for (int j = 0; j < samplesList.Count; j++)
                {
                    if (samplesList[j].Count > i && samplesList.Count > j)
                    {
                        onlineStreams[j].AddSamples(samplesList[j][i]);
                        streams.Add(onlineStreams[j]);
                        isEndpoints[0] = false;
                    }
                    else
                    {
                        streams.Add(onlineStreams[j]);
                        samplesList.Remove(samplesList[j]);
                        isEndpoints[0] = true;
                    }
                }
                for (int j = 0; j < samplesList.Count; j++)
                {
                    if (isEndpoints[j])
                    {
                        if (onlineStreams[j].IsFinished(isEndpoints[j]))
                        {
                            isEnds[j] = true;
                        }
                        else
                        {
                            streams.Add(onlineStreams[j]);
                        }
                    }
                }
                List<WenetAsr.OnlineRecognizerResultEntity> results_batch = onlineRecognizer.GetResults(streams);
                foreach (WenetAsr.OnlineRecognizerResultEntity result in results_batch)
                {
                    Console.WriteLine(result.text);
                    //Console.WriteLine("");
                }
                Console.WriteLine("");
                i++;
                bool isAllFinish = true;
                for (int j = 0; j < samplesList.Count; j++)
                {
                    if (!isEnds[j])
                    {
                        isAllFinish = false;
                        break;
                    }
                }
                if (isAllFinish)
                {
                    break;
                }
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
