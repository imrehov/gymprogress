
const API = "http://localhost:5024"//process.env.NEXT_PUBLIC_API_URL!; // e.g. http://localhost:5000

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
