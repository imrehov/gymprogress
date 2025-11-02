import { getWorkout } from "@/lib/api";
import SetEditor from "@/components/SetEditor";

export default async function WorkoutPage({ params }: { params: { id: string } }) {
	const w = await getWorkout(params.id); //await the promise here?

	return (
		<main className="p-8 space-y-6">
			<header>
				<h1 className="text-2xl font-bold">Workout on {w.date}</h1>
				{w.notes && <p className="text-sm text-muted-foreground">{w.notes}</p>}
			</header>

			{/*here pass data to seteditor client comp*/}

			<SetEditor workoutId={w.id} initialExercises={w.exercises} />
		</main>
	);
}

