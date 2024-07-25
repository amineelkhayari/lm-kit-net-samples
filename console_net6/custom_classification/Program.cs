﻿using LMKit.Model;
using LMKit.TextAnalysis;
using System.Diagnostics;
using System.Text;


namespace custom_classification
{
    internal class Program
    {
        static readonly string[] CLASSIFICATION_CATEGORIES = {
            "food and recipes",
            "technology",
            "health",
            "sport",
            "politics",
            "business",
            "environment",
            "movies and TV shows",
            "books and literature"
        };
        static readonly string DEFAULT_LLAMA3_8B_MODEL_PATH = @"https://huggingface.co/lm-kit/llama-3-8b-instruct-gguf/resolve/main/Llama-3-8B-Instruct-Q4_K_M.gguf";
        static readonly string DEFAULT_GEMMA2_9B_MODEL_PATH = @"https://huggingface.co/lm-kit/gemma-2-9b-gguf/resolve/main/gemma-2-9B-Q4_K_M.gguf";
        static readonly string DEFAULT_PHI3_MINI_3_8B_MODEL_PATH = @"https://huggingface.co/lm-kit/phi-3-instruct-gguf/resolve/main/Phi-3.1-mini-4k-Instruct-Q4_K_M.gguf";
        static readonly string DEFAULT_QWEN2_7_6B_MODEL_PATH = @"https://huggingface.co/lm-kit/qwen-2-7.6b-instruct-gguf/resolve/main/Qwen-2-7.6B-Instruct-Q4_K_M.gguf";
        static readonly string DEFAULT_MISTRAL_NEMO_12_2B_MODEL_PATH = @"https://huggingface.co/lm-kit/mistral-nemo-2407-12.2b-instruct-gguf/resolve/main/Mistral-Nemo-2407-12.2B-Instruct-Q4_K_M.gguf";
        static bool _isDownloading;

        private static bool ModelDownloadingProgress(string path, long? contentLength, long bytesRead)
        {
            _isDownloading = true;
            if (contentLength.HasValue)
            {
                double progressPercentage = Math.Round((double)bytesRead / contentLength.Value * 100, 2);
                Console.Write($"\rDownloading model {progressPercentage:0.00}%");
            }
            else
            {
                Console.Write($"\rDownloading model {bytesRead} bytes");
            }

            return true;
        }

        private static bool ModelLoadingProgress(float progress)
        {
            if (_isDownloading)
            {
                Console.Clear();
                _isDownloading = false;
            }

            Console.Write($"\rLoading model {Math.Round(progress * 100)}%");

            return true;
        }

        static void Main(string[] args)
        {
            LMKit.Licensing.LicenseManager.SetLicenseKey(""); //set an optional license key here if available.
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            Console.Clear();
            Console.WriteLine("Please select the model you want to use:\n");
            Console.WriteLine("0 - Mistral Nemo 2407 12.2B (requires approximately 7.7 GB of VRAM)");
            Console.WriteLine("1 - Meta Llama 3 8B (requires approximately 6 GB of VRAM)");
            Console.WriteLine("2 - Google Gemma2 9B Medium (requires approximately 7 GB of VRAM)");
            Console.WriteLine("3 - Microsoft Phi-3 3.82B Mini (requires approximately 3.3 GB of VRAM)");
            Console.WriteLine("4 - Alibaba Qwen-2 7.6B (requires approximately 5.6 GB of VRAM)");
            Console.Write("Other entry: A custom model URI\n\n> ");

            string input = Console.ReadLine();
            string modelLink;

            switch (input.Trim())
            {
                case "0":
                    modelLink = DEFAULT_MISTRAL_NEMO_12_2B_MODEL_PATH;
                    break;
                case "1":
                    modelLink = DEFAULT_LLAMA3_8B_MODEL_PATH;
                    break;
                case "2":
                    modelLink = DEFAULT_GEMMA2_9B_MODEL_PATH;
                    break;
                case "3":
                    modelLink = DEFAULT_PHI3_MINI_3_8B_MODEL_PATH;
                    break;
                case "4":
                    modelLink = DEFAULT_QWEN2_7_6B_MODEL_PATH;
                    break;
                default:
                    modelLink = input.Trim().Trim('"');;
                    break;
            }

            //Loading model
            Uri modelUri = new Uri(modelLink);
            LLM model = new LLM(modelUri,
                                    downloadingProgress: ModelDownloadingProgress,
                                    loadingProgress: ModelLoadingProgress);

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"Please enter a text to be classified within one of these categories:\n{String.Join(", ", CLASSIFICATION_CATEGORIES)}.");
            Console.ResetColor();

            Categorization classifier = new Categorization(model);


            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"\n\nContent: ");
                Console.ResetColor();

                string text = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(text))
                {
                    break;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"\nCategory: ");
                Console.ResetColor();

                Stopwatch sw = Stopwatch.StartNew();
                int categoryIndex = classifier.GetBestCategory(CLASSIFICATION_CATEGORIES, text);
                sw.Stop();
                if (categoryIndex != -1)
                {
                    Console.Write(CLASSIFICATION_CATEGORIES[categoryIndex]);
                }
                else
                {
                    Console.Write("Unknown");
                }

                Console.WriteLine($" - Elapsed: {Math.Round(sw.Elapsed.TotalSeconds, 2)} seconds - Confidence: {Math.Round(classifier.Confidence * 100, 1)} %");
            }

            Console.WriteLine("The program ended. Press any key to exit the application.");
            _ = Console.ReadKey();
        }
    }
}