<?php

namespace Database\Seeders;

use Illuminate\Database\Seeder;
use App\Models\Question;

class QuestionSeeder extends Seeder
{
    public function run(): void
    {
        $steps = [
            'Project Info',
            'Initiation',
            'Planning',
            'Execution',
            'Monitoring',
            'Closing',
        ];

        foreach ($steps as $step) {
            Question::firstOrCreate(
                ['step_name' => $step],
                ['enabled' => true]
            );
        }
    }
}
