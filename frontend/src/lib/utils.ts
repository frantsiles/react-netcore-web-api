import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

/*
  cn() combina clsx + tailwind-merge.
  - clsx: une clases condicionalmente  → cn('a', condition && 'b')
  - twMerge: resuelve conflictos Tailwind → cn('px-2', 'px-4') => 'px-4'
*/
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
