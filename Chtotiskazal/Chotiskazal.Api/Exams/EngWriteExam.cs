﻿using System;
using System.Linq;
using Chotiskazal.Api.Models;
using Chotiskazal.ConsoleTesting.Services;

namespace Chotiskazal.ApI.Exams
{
    public class EngWriteExam : IExam
    {
        public bool NeedClearScreen => false;

        public string Name => "Eng Write";

        public ExamResult Pass(ExamService service, WordForLearning word, WordForLearning[] examList)
        {
            
            var translations = word.GetTranslations();
            var minCount = translations.Min(t => t.Count(c => c == ' '));
            if (minCount>0 && word.PassedScore< minCount*4)
                return ExamResult.Impossible;


            Console.WriteLine("=====>   " + word.OriginWord + "    <=====");

            Console.Write("Write the translation: ");
            var translation = Console.ReadLine();
            if (string.IsNullOrEmpty(translation))
                return ExamResult.Retry;

            if (translations.Any(t => string.Compare(translation, t, StringComparison.OrdinalIgnoreCase) == 0))
            {
                service.RegistrateSuccess(word.MetricId);
                return ExamResult.Passed;
            }
            else
            {
                if (word.GetAllMeanings()
                    .Any(t => string.Compare(translation, t, StringComparison.OrdinalIgnoreCase) == 0))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Choosen translation is out of scope (but it is correct). Expected translations are: " + word.Translations);
                    Console.ResetColor();
                    Console.WriteLine();
                    return ExamResult.Impossible;
                }

                Console.WriteLine("The translation was: "+ word.Translations);
                service.RegistrateFailure(word.MetricId);
                return ExamResult.Failed;
            }
        }
    }
}