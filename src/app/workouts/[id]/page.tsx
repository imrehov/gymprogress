import { getWorkout } from "@/lib/api";

export default async function WorkoutPage({ params }: { params: { id: string } }) {
	const w = await getWorkout(params.id); //await the promise here?

	return (
		<main className="p-8">
			<h1 className="text-2xl font-bold">{w.date}'s workout</h1>
			{w.exercises.map(ex => (
				<section key={ex.id}>
					<h2 className="font-semibold mt-4">{ex.id}</h2>
					<ul className="list-disc pl-6">
						{ex.sets.map(s => (
							<li key={s.id}>{s.reps} reps {s.weight ? `@ ${s.weight}` : ''} {s.rpe ? `(RPE ${s.rpe})` : ''}</li>
						))}
					</ul>
				</section>
			))}
		</main>
	);
}

