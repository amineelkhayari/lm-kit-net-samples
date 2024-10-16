﻿using LMKit.Data;
using LMKit.Extraction;
using LMKit.Model;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace structured_data_extraction
{
    internal class Program
    {
        static readonly string DEFAULT_LLAMA3_1_8B_MODEL_PATH = @"https://huggingface.co/lm-kit/llama-3.1-8b-instruct-gguf/resolve/main/Llama-3.1-8B-Instruct-Q4_K_M.gguf?download=true";
        static readonly string DEFAULT_GEMMA2_9B_MODEL_PATH = @"https://huggingface.co/lm-kit/gemma-2-9b-gguf/resolve/main/gemma-2-9B-Q4_K_M.gguf?download=true";
        static readonly string DEFAULT_PHI3_5_MINI_3_8B_MODEL_PATH = @"https://huggingface.co/lm-kit/phi-3.5-mini-3.8b-instruct-gguf/resolve/main/Phi-3.5-mini-Instruct-Q4_K_M.gguf?download=true";
        static readonly string DEFAULT_QWEN2_5_7B_MODEL_PATH = @"https://huggingface.co/lm-kit/qwen-2.5-7b-instruct-gguf/resolve/main/Qwen-2.5-7B-Instruct-Q4_K_M.gguf?download=true";
        static readonly string DEFAULT_MISTRAL_NEMO_12_2B_MODEL_PATH = @"https://huggingface.co/lm-kit/mistral-nemo-2407-12.2b-instruct-gguf/resolve/main/Mistral-Nemo-2407-12.2B-Instruct-Q4_K_M.gguf?download=true";
        static readonly string DEFAULT_LLAMA_3_2_1B_MODEL_PATH = @"https://huggingface.co/lm-kit/llama-3.2-1b-instruct.gguf/resolve/main/Llama-3.2-1B-Instruct-Q4_K_M.gguf?download=true";
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

        private static void Main(string[] args)
        {
            LMKit.Licensing.LicenseManager.SetLicenseKey(""); //set an optional license key here if available.
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            Console.Clear();

            Console.WriteLine("Please select the model you want to use:\n");
            Console.WriteLine("0 - Mistral Nemo 2407 12.2B (requires approximately 7.7 GB of VRAM)");
            Console.WriteLine("1 - Meta Llama 3.1 8B (requires approximately 6 GB of VRAM)");
            Console.WriteLine("2 - Google Gemma2 9B Medium (requires approximately 7 GB of VRAM)");
            Console.WriteLine("3 - Microsoft Phi-3.5 3.82B Mini (requires approximately 3.3 GB of VRAM)");
            Console.WriteLine("4 - Alibaba Qwen-2.5 7.6B (requires approximately 5.6 GB of VRAM)");
            Console.WriteLine("5 - Meta Llama 3.2 1B (requires approximately 1 GB of VRAM)");
            Console.Write("Other entry: A custom model URI\n\n> ");

            string input = Console.ReadLine();
            string modelLink;

            switch (input.Trim())
            {
                case "0":
                    modelLink = DEFAULT_MISTRAL_NEMO_12_2B_MODEL_PATH;
                    break;
                case "1":
                    modelLink = DEFAULT_LLAMA3_1_8B_MODEL_PATH;
                    break;
                case "2":
                    modelLink = DEFAULT_GEMMA2_9B_MODEL_PATH;
                    break;
                case "3":
                    modelLink = DEFAULT_PHI3_5_MINI_3_8B_MODEL_PATH;
                    break;
                case "4":
                    modelLink = DEFAULT_QWEN2_5_7B_MODEL_PATH;
                    break;
                case "5":
                    modelLink = DEFAULT_LLAMA_3_2_1B_MODEL_PATH;
                    break;
                default:
                    modelLink = input.Trim().Trim('"'); ;
                    break;
            }

            //Loading model
            Uri modelUri = new Uri(modelLink);
            LLM model = new LLM(modelUri,
                                    downloadingProgress: ModelDownloadingProgress,
                                    loadingProgress: ModelLoadingProgress);


            TextExtraction textExtraction = new TextExtraction(model);

            while (true)
            {
                Console.Clear();
                Console.WriteLine("Please select the content from which you want to extract structured data:\n");
                Console.WriteLine("0 - invoice.txt (simple extraction)");
                Console.WriteLine("1 - invoice.txt (extended extraction)");
                Console.WriteLine("2 - job_offer.txt");
                Console.WriteLine("3 - medical_record.txt");
                Console.Write("\n> ");
                input = Console.ReadLine();
                string inputFileName = "";

                switch (input.Trim())
                {
                    case "0":
                        textExtraction.Elements = CreateInvoiceElements(extended: false);
                        inputFileName = "invoice.txt";
                        break;
                    case "1":
                        textExtraction.Elements = CreateInvoiceElements(extended: true);
                        inputFileName = "invoice.txt";
                        break;
                    case "2":
                        textExtraction.Elements = CreateJobOfferElements();
                        inputFileName = "job_offer.txt";
                        break;
                    case "3":
                        textExtraction.Elements = CreateMedicalRecordElements();
                        inputFileName = "medical_record.txt";
                        break;
                    default:
                        continue;
                }

                Console.Clear();

                textExtraction.SetContent(File.ReadAllText($"examples/{inputFileName}"));

                Process.Start(new ProcessStartInfo(Path.Combine("examples", inputFileName))
                {
                    Verb = "open",
                    UseShellExecute = true
                });


                Console.WriteLine("Extracting content...\n");
                Stopwatch sw = Stopwatch.StartNew();
                var result = textExtraction.Parse();
                sw.Stop();

                Console.WriteLine("\nExtracted elements:\n");

                foreach (var item in result.Elements)
                {
                    Console.Write($"{item.TextExtractionElement.Name}: ");
                    WriteColor($"{item.ToString()}", ConsoleColor.Blue);
                }

                Console.WriteLine("\nJSON:\n\n" + result.Json);

                Console.WriteLine("\nExtraction done in " + sw.Elapsed.TotalSeconds.ToString() + " seconds.");

                Console.WriteLine("Hit any key to continue");
                Console.ReadKey();
            }

        }

        private static void WriteColor(string text, ConsoleColor color, bool addNL = true)
        {
            Console.ForegroundColor = color;
            if (addNL)
            {
                Console.WriteLine(text);
            }
            else
            {
                Console.Write(text);
            }

            Console.ResetColor();
        }

        private static List<TextExtractionElement> CreateInvoiceElements(bool extended)
        {
            List<TextExtractionElement> elements = new List<TextExtractionElement>();

            // Invoice details
            elements.Add(new TextExtractionElement("Invoice Reference", ElementType.String, "Unique identifier for the invoice."));
            elements.Add(new TextExtractionElement("Date", ElementType.Date, "The date the invoice was generated."));
            elements.Add(new TextExtractionElement("Due Date", ElementType.Date, "The deadline for payment of the invoice."));

            if (!extended)
            {
                elements.Add(new TextExtractionElement("Vendor Name", ElementType.String));
            }
            else
            {
                // Items in the invoice
                elements.Add(new TextExtractionElement(
                    "Items",
                    new List<TextExtractionElement>
                    {
                    new TextExtractionElement("Description", ElementType.String, "Detailed description of the item or service."),
                    new TextExtractionElement("Quantity", ElementType.Integer, "Number of units of the item or service."),
                    new TextExtractionElement("Unit Price", ElementType.Float, "Price per unit of the item or service."),
                    new TextExtractionElement("Total", ElementType.Float, "Total cost for the item (Quantity x Unit Price).")
                    },
                    isArray: true,
                    "List of all items or services included in the invoice."
                ));

                // Customer details
                elements.Add(new TextExtractionElement(
                    "Customer",
                    new List<TextExtractionElement>
                    {
                    new TextExtractionElement("Name", ElementType.String, "Full name of the customer."),
                    new TextExtractionElement("Email", ElementType.String, "Customer's email address."),
                    new TextExtractionElement(
                        "Postal Address",
                        new List<TextExtractionElement>
                        {
                            new TextExtractionElement("Street Address", ElementType.String),
                            new TextExtractionElement("Postal Code", ElementType.String),
                            new TextExtractionElement("City", ElementType.String),
                            new TextExtractionElement("Country", ElementType.String)
                        },
                        isArray: false
                    ),
                    new TextExtractionElement("Phone Number", ElementType.String, "Customer's phone number.")
                                },
                                isArray: false,
                                "Detailed information about the customer."
                            ));

                // Vendor details
                elements.Add(new TextExtractionElement(
                    "Vendor",
                    new List<TextExtractionElement>
                    {
                    new TextExtractionElement("Name", ElementType.String, "Vendor's business name."),
                    new TextExtractionElement("Email", ElementType.String, "Vendor's contact email."),
                    new TextExtractionElement("Country", ElementType.String, "Country where the vendor is located."),
                    new TextExtractionElement(
                        "Postal Address",
                        new List<TextExtractionElement>
                        {
                            new TextExtractionElement("Street Address", ElementType.String),
                            new TextExtractionElement("Postal Code", ElementType.String),
                            new TextExtractionElement("City", ElementType.String),
                            new TextExtractionElement("Country", ElementType.String)
                        },
                        isArray: false
                    ),
                    new TextExtractionElement("Phone Number", ElementType.String, "Vendor's contact phone number.")
                    },
                    isArray: false,
                    "Detailed information about the vendor."
                ));

                // Payment information
                elements.Add(new TextExtractionElement(
                    "Payment Information",
                    new List<TextExtractionElement>
                    {
                    new TextExtractionElement("Bank Name", ElementType.String, "Name of the vendor's bank."),
                    new TextExtractionElement("Bank Account No", ElementType.String, "Vendor's bank account number."),
                    new TextExtractionElement("IBAN", ElementType.String, "International Bank Account Number (IBAN) for the payment."),
                    new TextExtractionElement("BIC", ElementType.String, "Bank Identifier Code (BIC) for international transactions.")
                    },
                    isArray: false,
                    "Details related to the payment method and bank information."
                ));
            }

            // Invoice totals and payment terms
            elements.Add(new TextExtractionElement("Subtotal", ElementType.Float, "Total cost of the invoice before taxes are applied."));
            elements.Add(new TextExtractionElement("VAT Percentage", ElementType.Float, "The percentage of Value Added Tax (VAT) applied to the subtotal."));
            elements.Add(new TextExtractionElement("VAT Amount", ElementType.Float, "The calculated VAT amount based on the VAT percentage."));
            elements.Add(new TextExtractionElement("Total Amount", ElementType.Float, "The total amount due, including VAT."));
            elements.Add(new TextExtractionElement("Currency", ElementType.String, "Currency in which the invoice amount is specified."));
            elements.Add(new TextExtractionElement("Payment Terms", ElementType.String, "The terms and conditions for payment of the invoice."));

            return elements;
        }

        private static List<TextExtractionElement> CreateJobOfferElements()
        {
            List<TextExtractionElement> elements = new List<TextExtractionElement>();

            elements.Add(new TextExtractionElement("Job Offer Reference", ElementType.String, "The offer ID provided to the candidate."));
            elements.Add(new TextExtractionElement("Date", ElementType.Date, "The date the job offer was extended."));
            elements.Add(new TextExtractionElement("Position", ElementType.String, "The role offered to the candidate."));
            elements.Add(new TextExtractionElement("Start Date", ElementType.Date, "The start date for the new position."));
            elements.Add(new TextExtractionElement("Salary", ElementType.Float, "The yearly compensation offered to the candidate."));
            elements.Add(new TextExtractionElement("Location", ElementType.String, "The location of the job (city, country)."));
            elements.Add(new TextExtractionElement("Company Name", ElementType.String, "The name of the company extending the offer."));
            elements.Add(new TextExtractionElement("Company Address", ElementType.String, "The full address of the company."));
            elements.Add(new TextExtractionElement("Job Description", ElementType.String, "A detailed overview of the responsibilities of the position."));
            elements.Add(new TextExtractionElement("Terms of Employment", ElementType.String, "The conditions under which the candidate will work."));
            elements.Add(new TextExtractionElement("Contact Email", ElementType.String, "The contact email for questions about the offer."));
            elements.Add(new TextExtractionElement("Contact Phone", ElementType.String, "The phone number for contact."));
            elements.Add(new TextExtractionElement("Signature", ElementType.String, "The official representative's signature on the offer letter."));

            return elements;
        }

        private static List<TextExtractionElement> CreateMedicalRecordElements()
        {
            List<TextExtractionElement> elements = new List<TextExtractionElement>();

            // Patient Information
            elements.Add(new TextExtractionElement("Patient Name", ElementType.String, "Full name of the patient."));
            elements.Add(new TextExtractionElement("Patient ID", ElementType.String, "Unique identifier for the patient."));
            elements.Add(new TextExtractionElement("Date of Birth", ElementType.Date, "Patient's date of birth."));
            elements.Add(new TextExtractionElement("Gender", ElementType.String, "Patient's gender."));

            // Medical History
            elements.Add(new TextExtractionElement(
                "Medical History",
                new List<TextExtractionElement>
                {
        new TextExtractionElement("Condition", ElementType.String, "Name of the medical condition or disease."),
        new TextExtractionElement("Diagnosis Date", ElementType.Date, "Date when the condition was diagnosed."),
        new TextExtractionElement("Treatment", ElementType.String, "Treatment or procedure administered for the condition.")
                },
                isArray: true,
                "A list of past medical conditions and treatments for the patient."
            ));

            // Vital Signs
            elements.Add(new TextExtractionElement(
                "Vital Signs",
                new List<TextExtractionElement>
                {
        new TextExtractionElement("Heart Rate", ElementType.Float, "Patient's heart rate in beats per minute."),
        new TextExtractionElement("Blood Pressure", ElementType.String, "Patient's blood pressure measurement."),
        new TextExtractionElement("Temperature", ElementType.Float, "Patient's body temperature in degrees Celsius."),
        new TextExtractionElement("Respiratory Rate", ElementType.Float, "Patient's breathing rate in breaths per minute.")
                },
                isArray: false,
                "Patient's vital signs recorded during the medical examination."
            ));

            // Current Medications
            elements.Add(new TextExtractionElement(
                "Medications",
                new List<TextExtractionElement>
                {
        new TextExtractionElement("Medication Name", ElementType.String, "Name of the medication prescribed."),
        new TextExtractionElement("Dosage", ElementType.String, "Dosage of the medication."),
        new TextExtractionElement("Frequency", ElementType.String, "Frequency of medication intake."),
        new TextExtractionElement("Start Date", ElementType.Date, "Date when the medication was started."),
        new TextExtractionElement("End Date", ElementType.Date, "Date when the medication should be stopped, if applicable.")
                },
                isArray: true,
                "A list of medications the patient is currently taking."
            ));

            // Allergies
            elements.Add(new TextExtractionElement(
                "Allergies",
                new List<TextExtractionElement>
                {
        new TextExtractionElement("Allergen", ElementType.String, "Substance that causes the allergic reaction."),
        new TextExtractionElement("Reaction", ElementType.String, "Description of the allergic reaction."),
        new TextExtractionElement("Severity", ElementType.String, "Severity of the allergic reaction (e.g., mild, moderate, severe).")
                },
                isArray: true,
                "A list of allergies the patient has."
            ));

            // Lab Results
            elements.Add(new TextExtractionElement(
                "Lab Results",
                new List<TextExtractionElement>
                {
        new TextExtractionElement("Test Name", ElementType.String, "Name of the laboratory test conducted."),
        new TextExtractionElement("Test Date", ElementType.Date, "Date the test was conducted."),
        new TextExtractionElement("Result", ElementType.String, "Result of the laboratory test."),
        new TextExtractionElement("Reference Range", ElementType.String, "Normal range for the test result.")
                },
                isArray: true,
                "A list of laboratory test results for the patient."
            ));

            return elements;
        }
    }
}