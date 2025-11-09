export interface WorkoutSummary {
	id: string;
	date: string; //YYYY-MM-DD
	notes?: string | null;
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
	sets: WorkoutSet[];
}

export interface WorkoutSet {
	id: string;
	reps: number;
	weight?: number | null;
	rpe?: number | null;
}

export type WorkoutDetail = Workout;
