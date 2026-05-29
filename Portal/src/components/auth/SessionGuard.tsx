'use client';

import React, { useCallback, useEffect, useRef, useState } from 'react';
import { AlertCircle, LogOut, ShieldAlert } from 'lucide-react';

import { useAuth } from '@/lib/auth/AuthProvider';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { useTranslations } from '@/hooks/useTranslations';

const INACTIVITY_TIMEOUT = 15 * 60 * 1000; // 15 min
const WARNING_BEFORE = 1 * 60 * 1000; // 1 min
const COUNTDOWN_START = 60; // seconds

export function SessionGuard({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading, signOut } = useAuth();
  const t = useTranslations('session');

  const [showWarning, setShowWarning] = useState(false);
  const [timeLeft, setTimeLeft] = useState(COUNTDOWN_START);

  const warningTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const countdownTimerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const clearTimers = useCallback(() => {
    if (warningTimerRef.current) {
      clearTimeout(warningTimerRef.current);
      warningTimerRef.current = null;
    }
    if (countdownTimerRef.current) {
      clearInterval(countdownTimerRef.current);
      countdownTimerRef.current = null;
    }
  }, []);

  const resetTimers = useCallback(() => {
    clearTimers();

    setShowWarning(false);
    setTimeLeft(COUNTDOWN_START);

    if (!isAuthenticated || isLoading) return;

    warningTimerRef.current = setTimeout(() => {
      setShowWarning(true);

      countdownTimerRef.current = setInterval(() => {
        setTimeLeft((prev) => {
          if (prev <= 1) {
            clearTimers();
            setShowWarning(false);
            signOut();
            return COUNTDOWN_START;
          }
          return prev - 1;
        });
      }, 1000);
    }, Math.max(0, INACTIVITY_TIMEOUT - WARNING_BEFORE));
  }, [clearTimers, isAuthenticated, isLoading, signOut]);

  useEffect(() => {
    if (isAuthenticated && !isLoading) {
      resetTimers();
    } else {
      clearTimers();
      setShowWarning(false);
      setTimeLeft(COUNTDOWN_START);
    }

    return () => {
      clearTimers();
    };
  }, [isAuthenticated, isLoading, clearTimers, resetTimers]);

  useEffect(() => {
    if (!isAuthenticated || isLoading) return;

    const events = ['mousedown', 'mousemove', 'keydown', 'scroll', 'touchstart', 'click'] as const;

    const handleActivity = () => {
      if (showWarning) return;
      resetTimers();
    };

    events.forEach((e) => document.addEventListener(e, handleActivity, { passive: true }));
    return () => events.forEach((e) => document.removeEventListener(e, handleActivity));
  }, [isAuthenticated, isLoading, resetTimers, showWarning]);

  function handleContinue() {
    resetTimers();
  }

  function handleLogout() {
    clearTimers();
    setShowWarning(false);
    signOut();
  }

  return (
    <>
      {children}

      <Dialog open={showWarning} onOpenChange={() => {}}>
        <DialogContent className="sm:max-w-md" onInteractOutside={(e) => e.preventDefault()}>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <AlertCircle className="h-5 w-5 text-yellow-500" />
              {t('expiringSoon')}
            </DialogTitle>
            <DialogDescription>{t('inactivityWarning', { seconds: timeLeft })}</DialogDescription>
          </DialogHeader>

          <div className="flex items-center gap-3 rounded-lg border border-yellow-500/30 bg-yellow-500/10 p-3 text-sm text-yellow-700 dark:text-yellow-400">
            <ShieldAlert className="h-4 w-4 shrink-0" />
            {t('willSignOutAutomatically')}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={handleLogout} className="gap-2">
              <LogOut className="h-4 w-4" />
              {t('logout')}
            </Button>
            <Button onClick={handleContinue}>{t('continueSession')}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
