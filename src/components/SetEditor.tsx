'use client';

import { useState, useMemo } from "react";
import { useRouter } from 'next/navigation';
import { createSet, deleteSet } from "@/lib/api";
import type { Exercise, Set } from '@/types/api';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';

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
	const [exerciseId, setExerciseId] = useState();
	const [reps, setReps] = useState<number>();
	const [weight, setWeight] = useState<number | ''>('');
	const [rpe, setRpe] = useState<number | ''>('');

	const totalSets = useMemo(
		() => exercises.reduce((acc, e) => acc + e.sets.length, 0),
		[exercises]
	);

	async function onAddSet() {
		if (!exerciseId || !reps || reps < 1) return;

		try {
			setBusy(true);
			const newSet: Set = await createSet(workoutId, {
				exerciseId,
				reps,
				weight: weight === '' ? undefined : Number(weight),
				rpe: rpe === '' ? undefined : Number(rpe),
			});

			// add set to local state if missing
			setExercises(prev => {
				const next = structuredClone(prev) as Exercise[];
				const idx = next.findIndex(e => e.id === exerciseId);
				if (idx === -1) {
					next.push({ id: exerciseId, sets: [newSet] });
				} else {
					next[idx].sets = [...next[idx].sets, newSet];
				}
				return next;
			});
			// opetional router refresh
			// router.refresh();

			//reset fields
			setReps(reps);
			setRpe('');
		} finally {
			setBusy(false);
		}
	}

	async function onDeleteSet(targetId: string, exerciseIdOfSet: string) {
		try {
			setBusy(true);
			await deleteSet(targetId);

			setExercises(prev => {
				const next = structuredClone(prev) as Exercise[];
				const idx = next.findIndex(e => e.id === exerciseIdOfSet);
				if (idx !== -1) {
					next[idx].sets = next[idx].sets.filter(s => s.id !== targetId);
					//remove exercise bucket if empty
					if (next[idx].sets.length === 0) next.splice(idx, 1);
				}
				return next;
			});
		} finally {
			setBusy(false);
		}
	}

	return (
		<section className="space-y6">
			{/* addset form*/}
			<div className="rounded-x1 border p-4 space-y-3">
				<h2 className="font-semibold">Add set</h2>
				<div className="grid grid-cols-2 md:grid-cols-5 gap-3">
					<Input
						placeholder="exercise name"
						value={exerciseId}
						onChange={e => setExerciseId(e.target.value)}
					/>
					<Input
						type="number"
						//min={1}
						placeholder="number of reps"
						value={reps}
						onChange={e => setReps(Number(e.target.value))}
					/>
					<Input
						type="number"
						step="0.5"
						placeholder="working weight"
						value={weight}
						onChange={e => setWeight(e.target.value === '' ? '' : Number(e.target.value))}
					/>
					<Input
						type="number"
						step="0.5"
						placeholder="RPE 0-10"
						value={rpe}
						onChange={e => setRpe(e.target.value === '' ? '' : Number(e.target.value))}
					/>
					<Button onClick={onAddSet} disabled={busy}>
						{busy ? 'Saving...' : 'Add set'}
					</Button>
				</div>
				<p className="text-sm text-muted-foreground">Total sets: {totalSets}</p>
			</div>

			{/*Rendersets gruped by exercise*/}
			<div className="space-y-6">
				{exercises.length === 0 ? (
					<p className="text-sm text-muted-foreground">No sets yet.</p>
				) : (
					exercises.map(ex => (
						<div key={ex.id} className="rounder-x1 border p-4">
							<h3 className="font-semibold mb-2">{ex.id}</h3>
							<ul className="space-y-2">
								{ex.sets.map(s => (
									<li key={s.id} className="flex items-center justify-between gap-3">
										<span className="text-sm">
											{s.reps} reps
											{s.weight != null ? ` @ ${s.weight}` : ''}
											{s.rpe != null ? ` (RPE ${s.rpe})` : ''}
										</span>
										<Button
											variant="secondary"
											size="sm"
											onClick={() => onDeleteSet(s.id, ex.id)}
											disabled={busy}
										>
											Delete
										</Button>
									</li>
								))}
							</ul>
						</div>
					))
				)}
			</div>
		</section>
	);
}


