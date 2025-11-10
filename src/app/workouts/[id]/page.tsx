'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { getWorkout } from '@/lib/api';
import SetEditor from '@/components/SetEditor';
import type { Workout } from '@/types/api';

export default function WorkoutPage() {
	const { id } = useParams<{ id: string }>();
	const router = useRouter();
	const [workout, setWorkout] = useState<Workout | null>(null);
	const [error, setError] = useState<string | null>(null);

	useEffect(() => {
		if (!id) return;

		(async () => {
			try {
				const w = await getWorkout(id as string);
				setWorkout(w);
			} catch (err: any) {
				console.error('Failed to load workout', err);
				setError('Failed to load workout');
				if (String(err?.message ?? '').includes('401')) {
					router.push('/login');
				}
			}
		})();
	}, [id, router]);

	if (error) return <main className="p-8">{error}</main>;
	if (!workout) return <main className="p-8">Loading…</main>;

	return (
		<main className="p-8 space-y-6">
			<header>
				<h1 className="text-2xl font-bold">Workout on {workout.date}</h1>
				{workout.notes && (
					<p className="text-sm text-muted-foreground">{workout.notes}</p>
				)}
			</header>

			<SetEditor workoutId={workout.id} initialExercises={workout.exercises} />
		</main>
	);
}
