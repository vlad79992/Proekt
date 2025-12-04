using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Microsoft.AspNetCore.Http;

namespace proekt.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TranscribeController : ControllerBase
    {
        private readonly ILogger<TranscribeController> _logger;

        public TranscribeController(ILogger<TranscribeController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> TranscribeAudio(IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
            {
                return BadRequest("No audio file provided");
            }

            // Создаем временные файлы
            string tempInputPath = Path.GetTempFileName();
            string tempWavPath = Path.GetTempFileName() + ".wav";

            try
            {
                // Сохраняем загруженный файл
                await using (var stream = new FileStream(tempInputPath, FileMode.Create))
                {
                    await audioFile.CopyToAsync(stream);
                }

                // Конвертируем в WAV 16kHz
                if (Path.GetExtension(audioFile.FileName).ToLower() != ".wav")
                {
                    ConvertToWav16kHz(tempInputPath, tempWavPath);
                }
                else
                {
                    // Для WAV файлов проверяем формат
                    using var reader = new WaveFileReader(tempInputPath);
                    if (reader.WaveFormat.SampleRate == 16000 && reader.WaveFormat.Channels == 1)
                    {
                        System.IO.File.Copy(tempInputPath, tempWavPath, true);
                    }
                    else
                    {
                        ConvertToWav16kHz(tempInputPath, tempWavPath);
                    }
                }

                // Обрабатываем через Whisper
                var transcription = await ProcessWithWhisper(tempWavPath);
                return Ok(new { Text = transcription });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during transcription");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
            finally
            {
                // Очищаем временные файлы
                try
                {
                    if (System.IO.File.Exists(tempInputPath))
                        System.IO.File.Delete(tempInputPath);
                    if (System.IO.File.Exists(tempWavPath))
                        System.IO.File.Delete(tempWavPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error cleaning up temporary files");
                }
            }
        }

        private async Task<string> ProcessWithWhisper(string wavFilePath)
        {
            Stopwatch sw = Stopwatch.StartNew();
            var resultBuilder = new StringBuilder();

            using var whisperFactory = WhisperFactory.FromPath("C:\\dev\\Test\\bin\\Debug\\net9.0\\whisper-large-v3-turbo");
            using var processor = whisperFactory.CreateBuilder()
                .WithLanguage("auto")
                .Build();

            await using var fileStream = System.IO.File.OpenRead(wavFilePath);

            await foreach (var result in processor.ProcessAsync(fileStream))
            {
                Console.WriteLine(result.Text);
                resultBuilder.AppendLine(Censore(result.Text));
            }

            sw.Stop();
            _logger.LogInformation($"Transcription completed in {sw.Elapsed.TotalSeconds} seconds");

            return resultBuilder.ToString().Trim();
        }

        private void ConvertToWav16kHz(string inputPath, string outputPath)
        {
            using (var mp3Reader = new MediaFoundationReader(inputPath))
            {
                var targetFormat = new WaveFormat(16000, 16, 1); // 16kHz, 16-bit, mono

                using (var resampler = new MediaFoundationResampler(mp3Reader, targetFormat))
                {
                    resampler.ResamplerQuality = 60;
                    WaveFileWriter.CreateWaveFile(outputPath, resampler);
                }
            }
        }

        public static string Censore(string line)
        {
            if (string.IsNullOrEmpty(line))
                return line;

            line = line.Replace("Блять", "*****")
                      .Replace("блять", "*****")
                      .Replace("Блядь", "*****")
                      .Replace("блядь", "*****")
                      .Replace("Хуй", "***")
                      .Replace("хуй", "***")
                      .Replace("Хую", "***")
                      .Replace("хую", "***")
                      .Replace("Хуя", "***")
                      .Replace("хуя", "***")
                      .Replace("Ебё", "***")
                      .Replace("ебё", "***")
                      .Replace("Еба", "***")
                      .Replace("еба", "***")
                      .Replace("Пизд", "****")
                      .Replace("пизд", "****");

            return line;
        }
    }
}