using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chotiskazal.ApI.Exams;
using Chotiskazal.Api.Services;
using Chotiskazal.Dal;
using Chotiskazal.DAL;
using Chotiskazal.Dal.Services;
using Chotiskazal.DAL.Services;
using Chotiskazal.LogicR;

namespace Chotiskazal.ConsoleTesting.Services
{
    public class ExamService
    {
        private ExamsAndMetricService _examsAndMetricService;
        private DictionaryService _dictionaryService;
        private UsersWordsService _usersWordsService;

        public ExamService( ExamsAndMetricService examsAndMetricService,
            DictionaryService dictionaryService,UsersWordsService usersWordsService)
        {
            _examsAndMetricService = examsAndMetricService;
            _dictionaryService = dictionaryService;
            _usersWordsService = usersWordsService;
        }

        public UserWordForLearning[] GetWordsForLearning(int userId, int count, int maxTranslationSize)
        {
            var wordsForLearning = _usersWordsService.GetWorstForUser(userId, count);

            foreach (var wordForLearning in wordsForLearning)
            {
                var translations = wordForLearning.GetTranslations().ToArray();
                if (translations.Length <= maxTranslationSize)
                    continue;

                var usedTranslations = translations.Randomize().Take(maxTranslationSize).ToArray();
                wordForLearning.SetTranslation(usedTranslations);


                // Remove Phrases added as learning word 
                  for (int i = 0; i < wordForLearning.Phrases.Count; i++)
                  {
                      var phrase = wordForLearning.Phrases[i];
                      if (!usedTranslations.Contains(phrase.PhraseRuTranslate))
                          wordForLearning.Phrases.RemoveAt(i);
                  }
            }
            return wordsForLearning.ToArray();
        }

        //TODO Зачем этот метод и метод выше, а еще GetTestWord
       public UserWordForLearning[] GetPairsForTestWords(int userId, int delta, int randomRate)
       {
             return _usersWordsService.GetWorstTestWordForUser(userId, delta,randomRate);
       }

       public new List<UserWordForLearning> PreparingExamsList(UserWordForLearning[] learningWords)
       {
           var examsList = new List<UserWordForLearning>(learningWords.Length * 4);
           //Every learning word appears in test from 2 to 4 times

           examsList.AddRange(learningWords.Randomize());
           examsList.AddRange(learningWords.Randomize());
           examsList.AddRange(learningWords.Randomize().Where(w => RandomTools.Rnd.Next() % 2 == 0));
           examsList.AddRange(learningWords.Randomize().Where(w => RandomTools.Rnd.Next() % 2 == 0));

           while (examsList.Count > 32)
           {
               examsList.RemoveAt(examsList.Count - 1);
           }
           return examsList;
       }
       public UserWordForLearning[] GetTestWords(int userId,List<UserWordForLearning> examsList)
       {
           //TODO изучть по какому принципу получаем RandomRATE. связан ли он с прогрессом подбираемых слов.
           //TODO Или тут вообще рандомные слова будут
           var delta = Math.Min(7, (32 - examsList.Count));
           UserWordForLearning[] testWords = new UserWordForLearning[0];
           if (delta > 0)
           {
               var randomRate = 8 + RandomTools.Rnd.Next(5);
               return testWords = GetPairsForTestWords(userId, delta, randomRate);
           }
           return testWords;
       }

       public void UpdateAgingAndRandomize(int i) => _usersWordsService.UpdateAgingAndRandomize(i);

       public void SaveQuestionMetrics(QuestionMetric questionMetric) =>
           _examsAndMetricService.SaveQuestionMetrics(questionMetric);
       
       public void RegistrateExam(int userId, DateTime started, int examsCount, int examsPassed) =>
           _examsAndMetricService.RegistrateExam(userId, started, examsCount, examsPassed);

       public void RegistrateSuccess(UserWordForLearning userWordForLearning) => 
           _usersWordsService.RegistrateSuccess(userWordForLearning);
      
       public void RegistrateFailure(UserWordForLearning userWordForLearning) =>
           _usersWordsService.RegistrateFailure(userWordForLearning);

       public QuestionMetric CreateQuestionMetric(UserWordForLearning pairModel, IExam exam)
       {
           var questionMetric = new QuestionMetric
           {
               WordId = pairModel.Id,
               Created=DateTime.Now,
               AggregateScoreBefore = pairModel.AggregateScore, 
               ExamsPassed = pairModel.Examed,
               PassedScoreBefore = pairModel.PassedScore,
               PreviousExam = pairModel.LastExam,
               Type = exam.Name,
           };
           return questionMetric;
       }

       public void RandomizationAndJobs(int userId)
       {
           if (RandomTools.Rnd.Next() % 30 == 0)
               AddMutualPhrasesToVocab(userId, 10);
           else
               UpdateAgingAndRandomize(50);
       }
       
       private void AddMutualPhrasesToVocab(int userId, int maxCount)
       {
           var allWords = _usersWordsService.GetAllWords(userId).Select(s => s.ToLower().Trim()).ToHashSet();
           var allWordsForLearning = _usersWordsService.GetAllUserWordsForLearning(userId);
        
           List<int> allPhrasesIdForUser = new List<int>();

           foreach (var word in allWordsForLearning)
           {
               var phrases = word.GetPhrasesId();
               allPhrasesIdForUser.AddRange(phrases);
           }

           var allPhrases = _dictionaryService.FindSeverealPrasesById(allPhrasesIdForUser.ToArray());
          
           List<Phrase> searchedPhrases = new List<Phrase>();
           int endings = 0;
           foreach (var phrase in allPhrases)
           {
               var phraseText = phrase.EnPhrase;
               int count = 0;
               int endingCount = 0;
               foreach (var word in phraseText.Split(new[] {' ', ','}))
               {
                   var lowerWord = word.Trim().ToLower();
                   if (allWords.Contains(lowerWord))
                       count++;
                   else if (word.EndsWith('s'))
                   {
                       var withoutEnding = lowerWord.Remove(lowerWord.Length - 1);
                       if (allWords.Contains(withoutEnding))
                           endingCount++;
                   }
                   else if (word.EndsWith("ed"))
                   {
                       var withoutEnding = lowerWord.Remove(lowerWord.Length - 2);

                       if (allWords.Contains(withoutEnding))
                           endingCount++;
                   }
                   else if (word.EndsWith("ing"))
                   {
                       var withoutEnding = lowerWord.Remove(lowerWord.Length - 3);

                       if (allWords.Contains(withoutEnding))
                           endingCount++;
                   }

                   if (count + endingCount > 1)
                   {
                       searchedPhrases.Add(phrase);
                       if (endingCount > 0)
                       {
                           endings++;
                       }

                       if (count + endingCount > 2)
                           Console.WriteLine(phraseText);
                       break;
                   }
               }
           }

           var firstPhrases = searchedPhrases.Randomize().Take(maxCount);
           foreach (var phrase in firstPhrases)
           {
               Console.WriteLine("Adding " + phrase.EnPhrase);
               
             _usersWordsService.AddPhraseAsWordToUserCollection(phrase);
          //cv    _usersWordsService.RemovePhrase(phrase.Id,userId);
           }

           Console.WriteLine($"Found: {searchedPhrases.Count}+{endings}");
       }

      
       
       //TODO additional methods
       //use for Graph Mode
       public UserWordForLearning[] GetAllExamedWords(in int userId)
       {
           throw new NotImplementedException();
       }
       //use for Graph mode
       public Exam[] GetAllExams()
       {
           throw new NotImplementedException();
       }
       
      // TODO use for RuWriteExam(now i don't use it) need to understand
     /*public WordForLearning Get(string word)
     {
         var pairFromDb = _usersWordService.;

       
         public PairModel GetOrNull(string word)
         {
             if (!File.Exists(DbFile))
                 return null;
             using (var cnn = SimpleDbConnection())
             {
                 cnn.Open();
                 var result = cnn.Query<PairModel>(
                     @"SELECT * FROM Words WHERE OriginWord = @word", new { word }).FirstOrDefault();
                 return result;
             }
         }
     }*/




       


       
    }
}
