# WenetConformerAsr
A C# library for decoding the Wenet ASR onnx model

����һ�����ڽ��� wenet asr onnx ģ�͵�c#�⣬ʹ��c#��д������.net6.0��֧���ڶ�ƽ̨������windows��linux��android��macos��ios�ȣ����롢���á�

����ʹ��maui��unoѸ�ٹ������ڶ�ƽ̨���е�Ӧ�ó���
##### ��Ŀ��WenetConformerAsr��WenetConformerAsr2������:
1.��֮ͬ����

����һ�������õķ�ʽһ������֧��streaming��non-streamingģ�͵Ľ��롣

2.��֮ͬ����

| library  | streaming��non-streamingģ�ͼ���ģ��  |ģ�ͺ���չ|
| ------------ | ------------ |------------|
| WenetConformerAsr  | �϶�Ϊһ  |1.����wenet�ٷ�������onnxģ�ͣ�������|
| WenetConformerAsr2  |���Զ���   |1.����wenet�ٷ�������onnxģ�͡�2.������չ������Լ�������streaming��non-streaming onnxģ�����ò���������ͬ�������ڸ��Ե�ģ���Ͻ��е���������Ӱ��|

���û�ж��ο�����������Ҫֱ��ʹ��wenet�ٵ�onnxģ�ͣ��Ƽ�ʹ��WenetConformerAsr.

##### ģ�͵�����
| ģ������  |  ���� | ʵʱ��RTF  | ֧������  | ���  |  ʱ��� | ���ص�ַ  |
| ------------ | ------------ | ------------ | ------------ | ------------ | ------------ | ------------ |
|  wenet-u2pp-conformer-aishell-onnx-online-20210601 | ��ʽ  | cpu-rtf-0.20  | ����  |  �� | ��  |[modelscope](https://www.modelscope.cn/models/manyeyes/wenet-u2pp-conformer-aishell-onnx-online-20210601 "modelscope") |
|  wenet-u2pp-conformer-wenetspeech-onnx-online-20220506 | ��ʽ | cpu-rtf-0.20  |  ���� |  ��  | ��  | [modelscope](https://www.modelscope.cn/models/manyeyes/wenet-u2pp-conformer-wenetspeech-onnx-online-20220506 "modelscope")  |

##### ģ�͵�ʹ��
����
```csharp
using WenetConformerAsr;
```
����offline-model (use non-streaming onnx model decoding)
```csharp
//load model
string applicationBase = AppDomain.CurrentDomain.BaseDirectory;
string modelName = "wenet_onnx_wenetspeech_u2pp_conformer_20220506_offline";
string encoderFilePath = applicationBase + "./" + modelName + "/encoder.quant.onnx";
string decoderFilePath = applicationBase + "./" + modelName + "/decoder.quant.onnx";
string ctcFilePath = applicationBase + "./" + modelName + "/ctc.quant.onnx";
string tokensFilePath = applicationBase + "./" + modelName + "/units.txt";
WenetConformerAsr.OfflineRecognizer offlineRecognizer = new WenetConformerAsr.OfflineRecognizer(encoderFilePath, decoderFilePath, ctcFilePath, tokensFilePath);
//����ʡ����Ƶ�ļ���sample��ת����������Բο�examples�е�test_WenetConformerAsrOfflineRecognizer
WenetConformerAsr.OfflineStream stream = offlineRecognizer.CreateOfflineStream();
stream.AddSamples(sample);
WenetConformerAsr.Model.OfflineRecognizerResultEntity result = offlineRecognizer.GetResult(stream);
Console.WriteLine(result.Text);
```
offline-model decoding rtf��0.12

����online-model (use streaming onnx model decoding)
```csharp
//load model
string applicationBase = AppDomain.CurrentDomain.BaseDirectory;
string modelName = "wenet_onnx_wenetspeech_u2pp_conformer_20220506_online";
string encoderFilePath = applicationBase + "./" + modelName + "/encoder.quant.onnx";
string decoderFilePath = applicationBase + "./" + modelName + "/decoder.quant.onnx";
string ctcFilePath = applicationBase + "./" + modelName + "/ctc.quant.onnx";
string tokensFilePath = applicationBase + "./" + modelName + "/units.txt";
WenetConformerAsr.OnlineRecognizer onlineRecognizer = new WenetConformerAsr.OnlineRecognizer(encoderFilePath, decoderFilePath, ctcFilePath, tokensFilePath);
//����ʡ����Ƶ�ļ���sample��ת����������������˷磬��������������Բο�examples�е�test_WenetConformerAsrOnlineRecognizer
WenetConformerAsr.OnlineStream stream = onlineRecognizer.CreateOnlineStream();
while (true)
{
    //����һ���򵥵Ľ���ʾ�⣬�����˽����ϸ���ܵ����̣���ο�examples
	//sample=������Ƶ�ļ�����˷�
    stream.AddSamples(sample);
    WenetConformerAsr.Model.OnlineRecognizerResultEntity result = onlineRecognizer.GetResult(stream);
    Console.WriteLine(result.Text);
}
```
online-model decoding rtf��0.22
