using LLama;
using LLama.Common;
using LLama.Sampling;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace proekt.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SummarizeController : ControllerBase // <-- Используйте ControllerBase вместо Controller
    {
        private readonly ILogger<SummarizeController> _logger;

        public SummarizeController(ILogger<SummarizeController> logger)
        {
            _logger = logger;
        }

        // Изменяем на POST и принимаем данные в теле запроса
        [HttpPost]
        public async Task<string> Summarize([FromBody] TextRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return "Error: Empty text";

            string modelPath = @"C:\Users\moshk\source\repos\MLShit\Models\QVikhr_GGUF\QVikhr-3-4B-Instruction-Q4_K_M.gguf";
            var sb = new StringBuilder();

            try
            {
                var parameters = new ModelParams(modelPath)
                {
                    GpuLayerCount = 5
                };
                using var model = await LLamaWeights.LoadFromFileAsync(parameters);
                var ex = new StatelessExecutor(model, parameters)
                {
                    ApplyTemplate = true,
                    SystemMessage = "Ты — ассистент, который точно пересказывает текст, сохраняя главную мысль. Максимально сокращай текст, используя только \"пользователь\". Тебе запрещено использовать слово \"я\" Не задавай вопросов и не добавляй комментарии. Будь как можно более краток в своих мыслях"

                };

                Console.ForegroundColor = ConsoleColor.Yellow;
                //Console.WriteLine("The executor has been enabled. In this example, the inference is an one-time job. That says, the previous input and response has " +
                //    "no impact on the current response. Now you can ask it questions. Note that in this example, no prompt was set for LLM and the maximum response tokens is 50. " +
                //    "It may not perform well because of lack of prompt. This is also an example that could indicate the importance of prompt in LLM. To improve it, you can add " +
                //    "a prompt for it yourself!");
                Console.ForegroundColor = ConsoleColor.White;

                var inferenceParams = new InferenceParams
                {
                    SamplingPipeline = new DefaultSamplingPipeline
                    {
                        Temperature = 0.7f
                    },

                    AntiPrompts = new List<string> { "Summarize:", },
                    MaxTokens = 2048
                };

                //Console.Write("\nQuestion: ");
                Console.ForegroundColor = ConsoleColor.Green;
                var prompt = request.Text;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Answer: ");
                prompt = $"Summarize this text: {prompt?.Trim()} \nSummary: ";
                await foreach (var text in ex.InferAsync(prompt, inferenceParams))
                {
                    Console.Write(text);
                    sb.Append(text);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации суммаризации");
                return $"Ошибка: {ex.Message}";
            }
            return sb.ToString();
        }

        // Модель запроса
        public class TextRequest
        {
            public string Text { get; set; } = string.Empty;
        }
    }
}
