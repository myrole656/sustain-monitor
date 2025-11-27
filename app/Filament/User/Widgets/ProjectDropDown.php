<?php

namespace App\Filament\User\Widgets;

use App\Models\Project;
use Filament\Widgets\Widget;
use Filament\Forms\Concerns\InteractsWithForms;
use Filament\Forms\Contracts\HasForms;
use Filament\Forms\Components\Select;
use Illuminate\Support\Facades\Auth;

class ProjectDropDown extends Widget implements HasForms
{
    use InteractsWithForms;

    protected  string $view = 'filament.user.widgets.project-dropdown';

    public ?int $projectId = null;

    public function mount()
    {
        $this->form->fill([
            'projectId' => null,
        ]);
    }

    protected function getFormSchema(): array
    {
        return [
            Select::make('projectId')
                ->label('Select Project')
                ->options(
                    Project::where('user_id', Auth::id())
                        ->pluck('project_name', 'id')
                        ->toArray()
                )
                ->placeholder('Choose project')
                ->reactive()
                ->afterStateUpdated(function ($state) {
                    $this->projectId = $state;

                    // Send event to SDG widget
                    $this->dispatch('projectSelected', projectId: $state);
                }),
        ];
    }
}
