'use client';

import { useState, useMemo } from "react";
import { useRouter } from 'next/navigation';
import { createSet, deleteSet } from '@/lib/api';
import type { Exercise, Set } from '@/types/api';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { todo } from "node:test";

type Props = {
	workoutId: string;
	initialExercises: Exercise[];
};

export default function SetEditor({ workoutId, initialExercises }: Props) {
	const router = useRouter();

	//local optimistic state
	const [exercises, setExercises] = useState<Exercise[]>(initialExercises);
	const [busy, setBusy] = useState(false);

	//simple form state
	//TODO remove placeholders
	const [exerciseId, setExerciseId] = useState('PLACEHOLDER');
	const [reps, setReps] = useState<number>(5);
	const [weight, setWeight] = useState<number | ''>('');
	const [rpe, setRpe] = useState<number | ''>('');

	const totalSets = useMemo(
		() => exercises.reduce((acc, e) => acc + e.sets.length, 0),
		[exercises]
	);
}

