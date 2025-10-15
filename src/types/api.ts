export interface WeeklySummary {
	weekStart: string;
	workouts: Array<Workout>;
	bodyWeightDelta: number;
}
export interface Workout {
	id: string;
	date: string;
	notes?: string;
	//bodyWeight: number;
	//exercises: Array<Exercise>;
}

export interface Exercise {
	id: string;
	sets: Array<Set>;
	lastTime: Set;
	monthBest: Set;
	allTimeBest: Set;
}

export interface Set {
	id: string;
	reps: number;
	weight?: number;
	rpe?: number;
}

