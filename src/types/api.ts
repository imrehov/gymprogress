export interface WeeklySummary {
	weekStart: string;
	workouts: Workout[];
	bodyWeightDelta: number;
}
export interface Workout {
	id: string;
	date: string // YYYY-MM-DD;
	notes?: string | null;
	bodyWeight: number;
	exercises: Exercise[];
}

export interface Exercise {
	id: string;
	sets: Set[];
}

export interface Set {
	id: string;
	reps: number;
	weight?: number | null;
	rpe?: number | null;
}

