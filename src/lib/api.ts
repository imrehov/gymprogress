import type { Workout, WorkoutSet } from "@/types/api";

const API = "http://localhost:5024"//process.env.NEXT_PUBLIC_API_URL!; // e.g. http://localhost:5000

function assertOk(r: Response) {
	if (!r.ok) throw new Error(`HTTP ${r.status}`);
	return r;
}

export async function listWorkouts(from: string, to: string) {
	const r = await fetch(`${API}/v1/workouts?from=${from}&to=${to}`, { cache: 'no-store' });
	//error if no 200 response
	if (!r.ok) throw new Error(`Failed to load (${r.status})`);
	return r.json();
}

export async function createWorkout(payload: { date: string; notes?: string }) {
	const r = await fetch(`${API}/v1/workouts`, {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify(payload),
	});
	if (!r.ok) {
		const text = await r.text().catch(() => '');
		throw new Error(`Failed to create (${r.status}) ${text}`);
	}
	return r.json();
}

export async function getWorkout(id: string): Promise<Workout> {
	const r = await fetch(`${API}/v1/workouts/${id}`, { cache: 'no-store' });
	assertOk(r);
	return r.json();
}


export async function createSet(workoutId: string, payload: { exerciseId: string; reps: number; weight?: number; rpe?: number }): Promise<WorkoutSet> {
	const r = await fetch(`${API}/v1/workouts/${workoutId}/sets`, {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify(payload),
	});
	assertOk(r);
	return r.json();
}


export async function deleteSet(setId: string): Promise<void> {
	const r = await fetch(`${API}/v1/sets/${setId}`, { method: 'DELETE' });
	assertOk(r);
}
