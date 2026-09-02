using System.Diagnostics;

namespace MathGame;

public partial class MainPage : ContentPage
{
    private const int QuestionsPerGame = 10;
    private const int SecondsPerQuestion = 30;
    private readonly Random _random = new();
    private readonly Stopwatch _questionStopwatch = new();
    private readonly IDispatcherTimer _timer;
    private readonly List<MathOperation> _selectedOperations = [];
    private int _currentQuestion;
    private int _correctAnswer;
    private int _correctAnswers;
    private int _wrongAnswers;
    private int _score;
    private int _secondsRemaining;
    private int _maximumOperand;
    private int _basePoints;
    private double _totalAnswerTime;
    private bool _isAcceptingAnswer;

    public MainPage()
    {
        InitializeComponent();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += Timer_Tick;
    }

    private async void StartGame_Clicked(object? sender, EventArgs e)
    {
        LoadSelectedOperations();
        if (_selectedOperations.Count == 0)
        {
            await DisplayAlertAsync("Selecione uma operação", "Escolha pelo menos uma operação para começar.", "OK");
            return;
        }

        ConfigureDifficulty();
        ResetGameStatistics();
        SetupCard.IsVisible = false;
        ResultCard.IsVisible = false;
        GameCard.IsVisible = true;
        GenerateQuestion();
    }

    private void LoadSelectedOperations()
    {
        _selectedOperations.Clear();
        if (AdditionCheckBox.IsChecked) _selectedOperations.Add(MathOperation.Addition);
        if (SubtractionCheckBox.IsChecked) _selectedOperations.Add(MathOperation.Subtraction);
        if (MultiplicationCheckBox.IsChecked) _selectedOperations.Add(MathOperation.Multiplication);
        if (DivisionCheckBox.IsChecked) _selectedOperations.Add(MathOperation.Division);
    }

    private void ConfigureDifficulty()
    {
        (_maximumOperand, _basePoints) = DifficultyPicker.SelectedIndex switch
        {
            1 => (50, 200),
            2 => (100, 300),
            _ => (10, 100)
        };
    }

    private void ResetGameStatistics()
    {
        _timer.Stop();
        _currentQuestion = 0;
        _correctAnswers = 0;
        _wrongAnswers = 0;
        _score = 0;
        _totalAnswerTime = 0;
        ScoreLabel.Text = "Pontos: 0";
    }

    private void GenerateQuestion()
    {
        _currentQuestion++;
        _isAcceptingAnswer = true;
        CheckAnswerButton.IsEnabled = true;
        AnswerEntry.IsEnabled = true;
        AnswerEntry.Text = string.Empty;
        FeedbackImage.Source = "question.png";
        FeedbackLabel.Text = "Resolva o desafio!";
        FeedbackLabel.TextColor = Color.FromArgb("#587083");
        QuestionProgressLabel.Text = $"Questão {_currentQuestion} de {QuestionsPerGame}";
        QuestionProgressBar.Progress = (double)_currentQuestion / QuestionsPerGame;

        MathOperation operation = _selectedOperations[_random.Next(_selectedOperations.Count)];
        int firstNumber;
        int secondNumber;
        string symbol;

        switch (operation)
        {
            case MathOperation.Addition:
                firstNumber = NextOperand();
                secondNumber = NextOperand();
                _correctAnswer = firstNumber + secondNumber;
                symbol = "+";
                break;
            case MathOperation.Subtraction:
                firstNumber = NextOperand();
                secondNumber = NextOperand();
                if (secondNumber > firstNumber) (firstNumber, secondNumber) = (secondNumber, firstNumber);
                _correctAnswer = firstNumber - secondNumber;
                symbol = "−";
                break;
            case MathOperation.Multiplication:
                firstNumber = NextOperand();
                secondNumber = NextOperand();
                _correctAnswer = firstNumber * secondNumber;
                symbol = "×";
                break;
            default:
                secondNumber = NextOperand();
                int maximumQuotient = Math.Max(1, _maximumOperand / secondNumber);
                _correctAnswer = _random.Next(1, maximumQuotient + 1);
                firstNumber = secondNumber * _correctAnswer;
                symbol = "÷";
                break;
        }

        QuestionLabel.Text = $"{firstNumber} {symbol} {secondNumber} = ?";
        StartQuestionTimer();
        AnswerEntry.Focus();
    }

    private int NextOperand() => _random.Next(1, _maximumOperand + 1);

    private void StartQuestionTimer()
    {
        _timer.Stop();
        _secondsRemaining = SecondsPerQuestion;
        _questionStopwatch.Restart();
        UpdateTimerDisplay();
        _timer.Start();
    }

    private async void Timer_Tick(object? sender, EventArgs e)
    {
        if (!_isAcceptingAnswer) return;
        _secondsRemaining--;
        UpdateTimerDisplay();
        if (_secondsRemaining <= 0) await FinishQuestionAsync(null, timedOut: true);
    }

    private void UpdateTimerDisplay()
    {
        TimerLabel.Text = $"{_secondsRemaining} segundo{(_secondsRemaining == 1 ? string.Empty : "s")}";
        TimerProgressBar.Progress = Math.Max(0, (double)_secondsRemaining / SecondsPerQuestion);
        string color = _secondsRemaining switch { <= 5 => "#C83E4D", <= 10 => "#E29022", _ => "#27845A" };
        TimerLabel.TextColor = Color.FromArgb(color);
        TimerProgressBar.ProgressColor = Color.FromArgb(color);
    }

    private async void CheckAnswer_Clicked(object? sender, EventArgs e) => await SubmitAnswerAsync();
    private async void AnswerEntry_Completed(object? sender, EventArgs e) => await SubmitAnswerAsync();

    private async Task SubmitAnswerAsync()
    {
        if (!_isAcceptingAnswer) return;
        if (!int.TryParse(AnswerEntry.Text, out int answer))
        {
            await DisplayAlertAsync("Resposta inválida", "Digite um número antes de conferir.", "OK");
            AnswerEntry.Focus();
            return;
        }
        await FinishQuestionAsync(answer, timedOut: false);
    }

    private void AnswerEntry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry || string.IsNullOrEmpty(e.NewTextValue)) return;
        string numericText = string.Concat(e.NewTextValue.Where(char.IsDigit));
        if (numericText != e.NewTextValue)
        {
            entry.Text = numericText;
            entry.CursorPosition = numericText.Length;
        }
    }

    private async Task FinishQuestionAsync(int? answer, bool timedOut)
    {
        if (!_isAcceptingAnswer) return;
        _isAcceptingAnswer = false;
        _timer.Stop();
        _questionStopwatch.Stop();
        CheckAnswerButton.IsEnabled = false;
        AnswerEntry.IsEnabled = false;
        _totalAnswerTime += Math.Min(_questionStopwatch.Elapsed.TotalSeconds, SecondsPerQuestion);

        bool isCorrect = !timedOut && answer == _correctAnswer;
        if (isCorrect)
        {
            _correctAnswers++;
            int speedBonus = _secondsRemaining * 5;
            _score += _basePoints + speedBonus;
            ScoreLabel.Text = $"Pontos: {_score}";
            FeedbackImage.Source = "win.png";
            FeedbackLabel.Text = $"Correto! +{_basePoints + speedBonus} pontos";
            FeedbackLabel.TextColor = Color.FromArgb("#27845A");
        }
        else
        {
            _wrongAnswers++;
            FeedbackImage.Source = "loose.png";
            FeedbackLabel.Text = timedOut
                ? $"Tempo esgotado! A resposta era {_correctAnswer}."
                : $"Incorreto. A resposta era {_correctAnswer}.";
            FeedbackLabel.TextColor = Color.FromArgb("#C83E4D");
        }

        await Task.Delay(1200);
        if (_currentQuestion < QuestionsPerGame) GenerateQuestion();
        else ShowFinalStatistics();
    }

    private void ShowFinalStatistics()
    {
        GameCard.IsVisible = false;
        ResultCard.IsVisible = true;
        int accuracy = _correctAnswers * 100 / QuestionsPerGame;
        ClassificationLabel.Text = accuracy switch
        {
            >= 90 => "Excelente! Você domina a matemática!",
            >= 70 => "Muito bom! Ótimo desempenho!",
            >= 50 => "Bom trabalho! Continue evoluindo!",
            _ => "Continue praticando. Você consegue!"
        };
        ResultImage.Source = accuracy >= 50 ? "win.png" : "loose.png";
        FinalScoreLabel.Text = _score.ToString();
        CorrectAnswersLabel.Text = $"{_correctAnswers}/{QuestionsPerGame}";
        WrongAnswersLabel.Text = _wrongAnswers.ToString();
        AverageTimeLabel.Text = $"{_totalAnswerTime / QuestionsPerGame:F1}s";
    }

    private void PlayAgain_Clicked(object? sender, EventArgs e)
    {
        ResultCard.IsVisible = false;
        SetupCard.IsVisible = true;
    }

    protected override void OnDisappearing()
    {
        _timer.Stop();
        base.OnDisappearing();
    }

    private enum MathOperation { Addition, Subtraction, Multiplication, Division }
}
