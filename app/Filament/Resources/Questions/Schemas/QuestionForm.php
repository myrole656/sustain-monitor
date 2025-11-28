<?php

namespace App\Filament\Resources\Questions\Schemas;
use App\Models\Question;
use Filament\Schemas\Schema;
use Filament\Forms\Components\TextInput;
use Filament\Forms\Components\Toggle;

class QuestionForm
{
    public static function configure(Schema $schema): Schema
    {
        return $schema->components([
            // Step name (read-only)
            TextInput::make('step_name')
                ->label('Step Name')
                ->required()
                ->disabled(), // Admin cannot rename the step

            // Enable / Disable step
            Toggle::make('enabled')
                ->label('Enable Step')
                ->default(true),
        ]);
    }
}
