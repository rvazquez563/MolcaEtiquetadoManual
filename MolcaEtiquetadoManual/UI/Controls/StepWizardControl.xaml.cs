// MolcaEtiquetadoManual/UI/Controls/StepWizardControl.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;

namespace MolcaEtiquetadoManual.UI.Controls
{
    /// <summary>
    /// Control para manejar un flujo de trabajo de pasos (wizard)
    /// </summary>
    public partial class StepWizardControl : UserControl
    {
        // Evento para notificar cambios de paso
        public event EventHandler<StepChangedEventArgs> StepChanged;

        // Propiedades para controlar el paso actual
        private int _currentStep = 1;
        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                // Si el paso no ha cambiado, no hacemos nada
                if (_currentStep == value)
                    return;

                // Guardar el paso anterior
                int previousStep = _currentStep;

                // Actualizar el paso actual
                _currentStep = value;

                // Asegurar que está en el rango válido
                if (_currentStep < 1)
                    _currentStep = 1;
                if (_currentStep > 3) // Máximo de 3 pasos
                    _currentStep = 3;

                // Actualizar UI para reflejar el paso actual
                UpdateUI();

                // Lanzar evento de cambio de paso
                OnStepChanged(new StepChangedEventArgs(previousStep, _currentStep));
            }
        }

        public StepWizardControl()
        {
            InitializeComponent();

            // Configuración inicial
            UpdateUI();
        }

        // Método para actualizar la interfaz según el paso actual
        private void UpdateUI()
        {
            // Actualizar indicadores de pasos
            step1Indicator.Tag = (CurrentStep >= 1) ? "Active" : "Inactive";
            step2Indicator.Tag = (CurrentStep >= 2) ? "Active" : "Inactive";
            step3Indicator.Tag = (CurrentStep == 3) ? "Active" : "Inactive";

            // Actualizar visibilidad de paneles
            // Actualizar visibilidad de paneles
            step1Panel.Visibility = (CurrentStep == 1) ? Visibility.Visible : Visibility.Collapsed;
            step2Panel.Visibility = (CurrentStep == 2) ? Visibility.Visible : Visibility.Collapsed;
            step3Panel.Visibility = (CurrentStep == 3) ? Visibility.Visible : Visibility.Collapsed;

            // Actualizar etiquetas de pasos
            step1Text.FontWeight = (CurrentStep == 1) ? FontWeights.Bold : FontWeights.Normal;
            step2Text.FontWeight = (CurrentStep == 2) ? FontWeights.Bold : FontWeights.Normal;
            step3Text.FontWeight = (CurrentStep == 3) ? FontWeights.Bold : FontWeights.Normal;

            // Notificar a los controles de contenido que se ha cambiado el paso para que puedan enfocar sus campos
            if (CurrentStep == 1 && step1Content.Children.Count > 0 && step1Content.Children[0] is Step1Control step1Control)
            {
                // Usamos BeginInvoke para asegurar que el enfoque ocurre después de que el UI se haya actualizado
                Dispatcher.BeginInvoke(new Action(() => step1Control.Reiniciar()));
            }
            else if (CurrentStep == 3 && step3Content.Children.Count > 0 && step3Content.Children[0] is Step3Control step3Control)
            {
                // Dar tiempo para que el panel sea visible antes de intentar enfocar
                Dispatcher.BeginInvoke(new Action(() => {
                    if (step3Control.txtCodigoVerificacion.IsEnabled)
                        step3Control.txtCodigoVerificacion.Focus();
                }));
            }
        }

        // Métodos para avanzar o retroceder en el flujo
        public void NextStep()
        {
            CurrentStep++;
        }

        public void PreviousStep()
        {
            CurrentStep--;
        }

        public void GoToStep(int step)
        {
            CurrentStep = step;
        }

        // Manejar evento de cambio de paso
        protected virtual void OnStepChanged(StepChangedEventArgs e)
        {
            StepChanged?.Invoke(this, e);
        }

        // Métodos para asignar contenido a cada paso
        public void SetStep1Content(UIElement content)
        {
            step1Content.Children.Clear();
            if (content != null)
                step1Content.Children.Add(content);
        }

        public void SetStep2Content(UIElement content)
        {
            step2Content.Children.Clear();
            if (content != null)
                step2Content.Children.Add(content);
        }

        public void SetStep3Content(UIElement content)
        {
            step3Content.Children.Clear();
            if (content != null)
                step3Content.Children.Add(content);
        }
    }

    // Clase para datos del evento de cambio de paso
    public class StepChangedEventArgs : EventArgs
    {
        public int PreviousStep { get; }
        public int NewStep { get; }

        public StepChangedEventArgs(int previousStep, int newStep)
        {
            PreviousStep = previousStep;
            NewStep = newStep;
        }
    }
}