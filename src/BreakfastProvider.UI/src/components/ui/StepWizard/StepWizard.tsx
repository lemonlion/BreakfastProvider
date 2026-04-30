'use client';

import { useState, type ReactNode } from 'react';
import { Button } from '../Button/Button';
import * as styles from './StepWizard.css';

interface Step {
  label: string;
  content: ReactNode;
  /** Async validation — return true to allow proceeding */
  validate?: () => Promise<boolean> | boolean;
}

interface StepWizardProps {
  steps: Step[];
  onComplete: () => void;
  /** Show step number indicators */
  showStepIndicator?: boolean;
}

/**
 * Multi-step form wizard.
 *
 * Learning point: The wizard manages step state independently from
 * TanStack Form. Each step's content can be a TanStack Form field group.
 * The validate callback lets each step run its own validation before
 * allowing the user to proceed to the next step.
 *
 * Used by the Order creation flow:
 * Step 1: Customer details → Step 2: Add items → Step 3: Review → Submit
 */
export function StepWizard({ steps, onComplete, showStepIndicator = true }: StepWizardProps) {
  const [currentStep, setCurrentStep] = useState(0);
  const [isValidating, setIsValidating] = useState(false);

  const isFirstStep = currentStep === 0;
  const isLastStep = currentStep === steps.length - 1;

  const handleNext = async () => {
    const step = steps[currentStep];
    if (step.validate) {
      setIsValidating(true);
      const isValid = await step.validate();
      setIsValidating(false);
      if (!isValid) return;
    }
    if (isLastStep) {
      onComplete();
    } else {
      setCurrentStep((prev) => prev + 1);
    }
  };

  return (
    <div className={styles.wrapper}>
      {/* Step indicators */}
      {showStepIndicator && (
        <div className={styles.indicators}>
          {steps.map((step, index) => (
            <div
              key={step.label}
              className={
                index === currentStep
                  ? styles.stepActive
                  : index < currentStep
                    ? styles.stepCompleted
                    : styles.stepPending
              }
            >
              <span className={styles.stepNumber}>{index + 1}</span>
              <span className={styles.stepLabel}>{step.label}</span>
            </div>
          ))}
        </div>
      )}

      {/* Current step content */}
      <div className={styles.content}>{steps[currentStep].content}</div>

      {/* Navigation buttons */}
      <div className={styles.actions}>
        <Button
          variant="secondary"
          onClick={() => setCurrentStep((prev) => prev - 1)}
          disabled={isFirstStep}
        >
          Back
        </Button>
        <Button variant="primary" onClick={handleNext} loading={isValidating}>
          {isLastStep ? 'Submit' : 'Next'}
        </Button>
      </div>
    </div>
  );
}
